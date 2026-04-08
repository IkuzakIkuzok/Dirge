
// (c) 2026 Kazuki Kohzuki

using Dirge.Utils;

namespace Dirge.Generators;

internal record DisposeGenerationInfo(DisposeGenerationStrategy Strategy, string AccessModifier)
{
    internal static DisposeGenerationInfo? Create(INamedTypeSymbol targetType, INamedTypeSymbol disposeSymbol, SourceProductionContext context, Compilation compilation)
    {
        var disposeMethod = GetBaseTypeMethod(targetType, "Dispose", VoidDispose);
        if (disposeMethod is null)
            return new(DisposeGenerationStrategy.GenerateRoot, "protected");

        var baseIsDisposable = targetType.ImplementsInterface(disposeSymbol);
        if (!baseIsDisposable)
        {
            // Base type has `public void Dispose()`, but does not implement `IDisposable`.
            DiagnosticReporter.DisposeInNonDisposableBase(context, targetType);
            return null;
        }

        var disposeBoolMethod = GetBaseTypeMethod(targetType, "Dispose", VirtualVoidDisposeBool);
        if (disposeMethod.IsAbstract)
            return new(DisposeGenerationStrategy.OverrideDispose, "protected");

        if (disposeBoolMethod is null || !compilation.IsSymbolAccessibleWithin(disposeBoolMethod, targetType))
        {
            // Base type has `public void Dispose()`, but no accessible `virtual void Dispose(bool)`.
            // Generating a new Dispose method is dangerous because it may cause resource leaks.
            DiagnosticReporter.MissingAccessibleDisposeBool(context, targetType);
            return null;
        }

        // Base type has accessible `virtual void Dispose(bool)`.
        return new(DisposeGenerationStrategy.OverrideDisposeBool, GetAccessibilityString(disposeBoolMethod.DeclaredAccessibility));
    } // internal static DisposeGenerationInfo? Create (INamedTypeSymbol, INamedTypeSymbol, SourceProductionContext, Compilation)

    private static IMethodSymbol? GetBaseTypeMethod(INamedTypeSymbol targetType, string name, Func<IMethodSymbol, bool> filter)
    {
        var baseType = targetType.BaseType;
        while (baseType is not null)
        {
            var method = baseType.GetMembers(name).OfType<IMethodSymbol>().FirstOrDefault(filter);
            if (method is not null) return method;
            baseType = baseType.BaseType;
        }
        return null;
    } // private static IMethodSymbol? GetMethod (INamedTypeSymbol, Func<IMethodSymbol, bool>)

    private static bool VoidDispose(IMethodSymbol method)
        => method.ReturnsVoid && method.Parameters.Length == 0;

    private static bool VirtualVoidDisposeBool(IMethodSymbol method)
        => method.ReturnsVoid
            && method.Parameters.Length == 1
            && method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean
            && (method.IsVirtual || method.IsAbstract || method.IsOverride);

    private static string GetAccessibilityString(Accessibility accessibility)
        => accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "protected"
        };
} // internal record DisposeGenerationInfo (DisposeGenerationStrategy, bool, string)
