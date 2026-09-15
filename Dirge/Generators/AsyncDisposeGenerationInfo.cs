// (c) 2026 Kazuki Kohzuki

using Dirge.Diagnostics;
using Dirge.Utils;

namespace Dirge.Generators;

internal record AsyncDisposeGenerationInfo(DisposeGenerationInfo SyncGeneration, bool OverrideCore, string CoreAccessibility, bool OverrideDisposeAsync = false)
{
    internal static Result<AsyncDisposeGenerationInfo> Create(INamedTypeSymbol target, INamedTypeSymbol disposable, INamedTypeSymbol asyncDisposable, Compilation compilation)
    {
        if (target.GetMembers().OfType<IMethodSymbol>().Any(m => m.Name is "Dispose" or "DisposeAsync" or "DisposeAsyncCore"
            || m.ExplicitInterfaceImplementations.Any(i => i.Name is "Dispose" or "DisposeAsync")))
            return DiagnosticDescriptors.InvalidAsyncDispose(target, "Dispose, DisposeAsync and DisposeAsyncCore must be generated, not declared on the target class");

        for (var baseType = target.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.GetAttributes().Any(a => a.AttributeClass.FullName == TypesGenerator.AutoDisposeAttributeName
                && a.TryGetNamedArgumentValue("IncludeAsync", out bool enabled) && enabled))
                return new AsyncDisposeGenerationInfo(new(DisposeGenerationStrategy.OverrideDisposeBool, "protected"), true, "protected");

            var hasDisposeMembers = baseType.GetMembers().OfType<IMethodSymbol>()
                .Any(m => m.Name is "Dispose" or "DisposeAsync" or "DisposeAsyncCore");
            if (!hasDisposeMembers && !baseType.ImplementsInterface(disposable) && !baseType.ImplementsInterface(asyncDisposable)
                && !baseType.GetAttributes().Any(a => a.AttributeClass.FullName == TypesGenerator.AutoDisposeAttributeName))
                continue;

            var dispose = FindMethod(baseType, "Dispose", m => m.Parameters.Length == 0 && m.ReturnsVoid);
            var disposeAsync = FindMethod(baseType, "DisposeAsync", m => m.Parameters.Length == 0 && IsValueTask(m.ReturnType));
            if (baseType.ImplementsInterface(disposable) && baseType.ImplementsInterface(asyncDisposable)
                && dispose is { IsAbstract: true, DeclaredAccessibility: Accessibility.Public }
                && disposeAsync is { IsAbstract: true, DeclaredAccessibility: Accessibility.Public })
                return new AsyncDisposeGenerationInfo(new(DisposeGenerationStrategy.OverrideDispose, "protected"), false, "protected", true);

            var syncCore = FindMethod(baseType, "Dispose", m => m.ReturnsVoid && m.Parameters.Length == 1
                && m.Parameters[0].Type.SpecialType == SpecialType.System_Boolean && m.Parameters[0].RefKind == RefKind.None);
            var asyncCore = FindMethod(baseType, "DisposeAsyncCore", m => m.Parameters.Length == 0 && IsValueTask(m.ReturnType));
            if (!baseType.ImplementsInterface(disposable) || !baseType.ImplementsInterface(asyncDisposable)
                || dispose is not { DeclaredAccessibility: Accessibility.Public, IsAbstract: false }
                || disposeAsync is not { DeclaredAccessibility: Accessibility.Public, IsAbstract: false }
                || !CanOverride(syncCore, target, compilation) || !CanOverride(asyncCore, target, compilation))
                return DiagnosticDescriptors.InvalidAsyncDispose(target,
                    "The disposable base class must implement both IDisposable and IAsyncDisposable with accessible, non-abstract, overridable Dispose(bool) and DisposeAsyncCore() methods");

            return new AsyncDisposeGenerationInfo(
                new(DisposeGenerationStrategy.OverrideDisposeBool, AccessibilityText(syncCore!, target)),
                true, AccessibilityText(asyncCore!, target));
        }

        return new AsyncDisposeGenerationInfo(new(DisposeGenerationStrategy.GenerateRoot, "protected"), false, "protected");
    }

    private static IMethodSymbol? FindMethod(INamedTypeSymbol type, string name, Func<IMethodSymbol, bool> predicate)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var method = current.GetMembers(name).OfType<IMethodSymbol>()
                .FirstOrDefault(m => !m.IsStatic && m.Arity == 0 && predicate(m));
            if (method is not null) return method;
        }
        return null;
    }

    private static bool IsValueTask(ITypeSymbol type)
        => type is INamedTypeSymbol { Arity: 0 } && type.ToDisplayString() == "System.Threading.Tasks.ValueTask";

    private static bool CanOverride(IMethodSymbol? method, INamedTypeSymbol target, Compilation compilation)
        => method is { IsSealed: false, IsAbstract: false }
            && (method.IsVirtual || method.IsOverride) && compilation.IsSymbolAccessibleWithin(method, target);

    private static string AccessibilityText(IMethodSymbol method, INamedTypeSymbol target)
        => method.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal when SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, target.ContainingAssembly) => "protected internal",
            _ => "protected"
        };
}
