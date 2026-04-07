
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Generators;

internal record TypeWrapperInfo(string Modifiers, string Keyword, string Name, string TypeParameters)
{
    private static readonly string[] _requiredKeywords = [
        "static", "abstract", "sealed", "readonly", "ref"
    ];

    internal string GetDeclaration()
        => $"{this.Modifiers}partial {this.Keyword} {this.Name}{this.TypeParameters}";

    internal static TypeWrapperInfo? FromTypeSymbol(INamedTypeSymbol symbol)
    {
        if (symbol.DeclaringSyntaxReferences.FirstOrDefault() is not { }  syntaxReference) return null;
        if (syntaxReference.GetSyntax() is not TypeDeclarationSyntax typeDeclaration) return null;

        var modifiers = typeDeclaration.Modifiers
            .Select(m => m.ValueText)
            .Where(keyword => _requiredKeywords.Contains(keyword));

        var modifiersString = string.Join(" ", modifiers);
        if (!string.IsNullOrEmpty(modifiersString))
        {
            modifiersString += " ";
        }

        var keyword = typeDeclaration.Keyword.ValueText;
        var typeParams = typeDeclaration.TypeParameterList?.ToString() ?? string.Empty;
        return new(modifiersString, keyword, symbol.Name, typeParams);
    } // internal static TypeWrapperInfo FromTypeSymbol (INamedTypeSymbol)
} // internal record TypeWrapperInfo (string, string, string, string)
