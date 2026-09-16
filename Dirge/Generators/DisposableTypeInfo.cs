
// (c) 2026 Kazuki Kohzuki

using Dirge.Diagnostics;
using Dirge.Utils;
using System.Collections.Generic;

namespace Dirge.Generators;

internal record DisposableTypeInfo(string Name, string? NamespaceName, EquatableArray<TypeWrapperInfo> DeclarationStack, bool IsSealed, bool IsRefLikeType, string? ReleaseUnmanagedResources, EquatableArray<DisposableFieldInfo> Fields, DisposeGenerationInfo GenerationInfo, string DisposedFieldName, string AsyncDisposedFieldName, AsyncDisposeGenerationInfo? AsyncGenerationInfo = null)
{
    internal static Result<DisposableTypeInfo>? Create(GeneratorAttributeSyntaxContext context)
    {
        var compilation = context.SemanticModel.Compilation;
        var disposableSymbol = compilation.GetTypeByMetadataName("System.IDisposable");
        if (disposableSymbol is null) return null;

        var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var attribute = context.Attributes.FirstOrDefault();
        if (attribute is null) return null;
        var includeAsync = attribute.TryGetNamedArgumentValue("IncludeAsync", out bool enabled) && enabled;
        var asyncDisposableSymbol = includeAsync ? compilation.GetTypeByMetadataName("System.IAsyncDisposable") : null;
        if (includeAsync && (asyncDisposableSymbol is null || compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask") is null))
            return DiagnosticDescriptors.InvalidAsyncDispose(targetSymbol, "The target framework must provide System.IAsyncDisposable and System.Threading.Tasks.ValueTask");
        if (includeAsync && targetSymbol.TypeKind != TypeKind.Class)
            return DiagnosticDescriptors.InvalidAsyncDispose(targetSymbol, "Only classes support generated asynchronous disposal");
        if (targetSymbol.IsReadOnly)
            return DiagnosticDescriptors.ReadonlyStructNotSupported(targetSymbol);

        if (targetSymbol.IsStatic)
            return DiagnosticDescriptors.StaticClassNotSupported(targetSymbol);

        var decl = targetSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();
        if (decl is null) return null;
        if (EnsureAllAncestorsArePartial(decl) is { } diagnostic)
            return diagnostic;

        if (!attribute.TryGetNamedArgumentValue("ReleaseUnmanagedResources", out string? releaseUnmanagedResources))
            releaseUnmanagedResources = null;

        var name = targetSymbol.Name;
        var namespaceName = targetSymbol.ContainingNamespace.IsGlobalNamespace ? null : targetSymbol.ContainingNamespace.ToDisplayString();
        var declarationStack = GetDeclarationStack(targetSymbol);
        var isSealed = targetSymbol.IsSealed;
        var isRefLikeType = targetSymbol.IsRefLikeType;

        var fieldResults = targetSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .SelectNotNull(f => DisposableFieldInfo.Create(f, targetSymbol, disposableSymbol, compilation, asyncDisposableSymbol))
            .ToArray();
        var fields = new DisposableFieldInfo[fieldResults.Length];
        var diagnostics = new List<DiagnosticInfo>();
        for (var i = 0; i < fieldResults.Length; i++)
        {
            var f = fieldResults[i];
            if (!f.IsSuccess)
                diagnostics.AddRange(f.Diagnostic!);
            fields[i] = f.Value!;
        }
        if (diagnostics.Count > 0)
            return diagnostics.ToArray();

        AsyncDisposeGenerationInfo? asyncGeneration = null;
        if (includeAsync)
        {
            var asyncResult = AsyncDisposeGenerationInfo.Create(targetSymbol, disposableSymbol, asyncDisposableSymbol!, compilation);
            if (!asyncResult.IsSuccess) return asyncResult.Diagnostic!;
            asyncGeneration = asyncResult.Value!;
        }

        var generationInfoResult = asyncGeneration is not null
            ? Result<DisposeGenerationInfo>.Success(asyncGeneration.SyncGeneration)
            : DisposeGenerationInfo.Create(targetSymbol, disposableSymbol, compilation, releaseUnmanagedResources);
        if (!generationInfoResult.IsSuccess)
            return generationInfoResult.Diagnostic!;
        
        var reservedNames = new HashSet<string>(targetSymbol.GetMembers().Select(member => member.Name), StringComparer.Ordinal)
        {
            name
        };
        foreach (var parameter in targetSymbol.TypeParameters)
            reservedNames.Add(parameter.Name);
        var disposedFieldName = GetUniqueFieldName("__generated_disposed", reservedNames);
        var asyncDisposedFieldName = GetUniqueFieldName("__generated_asyncDisposed", reservedNames);

        return new DisposableTypeInfo(name, namespaceName, declarationStack, isSealed, isRefLikeType, releaseUnmanagedResources, fields, generationInfoResult.Value!, disposedFieldName, asyncDisposedFieldName, asyncGeneration);
    } // internal static DisposableTypeInfo? Create (GeneratorAttributeSyntaxContext)

    private static string GetUniqueFieldName(string preferredName, HashSet<string> reservedNames)
    {
        // GetMembers includes all partial declarations. Inherited members may be
        // hidden by these private fields, as they were before collision avoidance.
        var candidate = preferredName;
        for (var suffix = 1; !reservedNames.Add(candidate); suffix++)
            candidate = preferredName + "_" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return candidate;
    } // private static string GetUniqueFieldName (string, HashSet<string>)

    private static DiagnosticInfo? EnsureAllAncestorsArePartial(TypeDeclarationSyntax typeDecl)
    {
        SyntaxNode? currentNode = typeDecl;

        while (currentNode is TypeDeclarationSyntax parentTypeDecl)
        {
            var isPartial = parentTypeDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

            if (!isPartial)
                return DiagnosticDescriptors.TypeMustBePartial(parentTypeDecl);

            currentNode = currentNode.Parent;
        }

        return null;
    } // private static DiagnosticInfo? EnsureAllAncestorsArePartial (TypeDeclarationSyntax)

    private static EquatableArray<TypeWrapperInfo> GetDeclarationStack(INamedTypeSymbol symbol)
    {
        var stack = new List<TypeWrapperInfo>();

        while (symbol is not null)
        {
            var info = TypeWrapperInfo.FromTypeSymbol(symbol);
            if (info is null)
                break;
            stack.Add(info);
            symbol = symbol.ContainingType;
        }

        return stack.ToArray();
    } // private static EquatableArray<string> GetDeclarationStack (TypeDeclarationSyntax)
} // internal record DisposableTypeInfo (string, string?, EquatableArray<TypeWrapperInfo>, bool, bool, string?, EquatableArray<DisposableFieldInfo>, DisposeGenerationInfo)
