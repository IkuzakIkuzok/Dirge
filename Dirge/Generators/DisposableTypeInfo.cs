
// (c) 2026 Kazuki Kohzuki

using Dirge.Diagnostics;
using Dirge.Utils;
using System.Collections.Generic;

namespace Dirge.Generators;

internal record DisposableTypeInfo(string Name, string? NamespaceName, EquatableArray<TypeWrapperInfo> DeclarationStack, bool IsSealed, bool HasDisposableBase, bool IsRefLikeType, string? ReleaseUnmanagedResources, EquatableArray<DisposableFieldInfo> Fields, DisposeGenerationInfo? GenerationInfo)
{
    internal static Result<DisposableTypeInfo>? Create(GeneratorAttributeSyntaxContext context)
    {
        var compilation = context.SemanticModel.Compilation;
        var disposableSymbol = compilation.GetTypeByMetadataName("System.IDisposable");
        if (disposableSymbol is null) return null;

        var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
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

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute is null) return null;

        if (!attribute.TryGetNamedArgumentValue("ReleaseUnmanagedResources", out string? releaseUnmanagedResources))
            releaseUnmanagedResources = null;

        var name = targetSymbol.Name;
        var namespaceName = targetSymbol.ContainingNamespace.IsGlobalNamespace ? null : targetSymbol.ContainingNamespace.ToDisplayString();
        var declarationStack = GetDeclarationStack(targetSymbol);
        var isSealed = targetSymbol.IsSealed;
        var hasDisposableBase = targetSymbol.ImplementsInterface(disposableSymbol);
        var isRefLikeType = targetSymbol.IsRefLikeType;

        var canBeSimple =
            isSealed
            && !hasDisposableBase
            && releaseUnmanagedResources is null;

        var fieldResults = targetSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .SelectNotNull(f => DisposableFieldInfo.Create(f, targetSymbol, disposableSymbol, compilation))
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

        DisposeGenerationInfo? generationInfo = null;
        if (!canBeSimple)
        {
            var generationInfoResult = DisposeGenerationInfo.Create(targetSymbol, disposableSymbol, compilation);
            if (!generationInfoResult.IsSuccess)
                return generationInfoResult.Diagnostic!;
            generationInfo = generationInfoResult.Value;
        }
        
        return new DisposableTypeInfo(name, namespaceName, declarationStack, isSealed, hasDisposableBase, isRefLikeType, releaseUnmanagedResources, fields, generationInfo);
    } // internal static DisposableTypeInfo? Create (GeneratorAttributeSyntaxContext)

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
} // internal record DisposableTypeInfo (string, string?, EquatableArray<TypeWrapperInfo>, bool, bool, bool, string?, EquatableArray<DisposableFieldInfo>, DisposeGenerationInfo?)
