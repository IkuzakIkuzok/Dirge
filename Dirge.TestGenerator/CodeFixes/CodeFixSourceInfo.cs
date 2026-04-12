
// (c) 2026 Kazuki Kohzuki

using Dirge.Utils;

namespace Dirge.TestGenerator.CodeFixes;

internal record CodeFixSourceInfo(string SourceName, string Source, string CodeFixType, string? TestName, string FilePath)
{
    private const string CodeFixSourceAttributeFullName = $"{TypesGenerator.Namespace}.{TypesGenerator.CodeFixSourceAttributeName}";

    internal static CodeFixSourceInfo? FromFieldSymbol(IFieldSymbol field)
    {
        if (field.ConstantValue is not string code) return null;

        var attr = field.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.FullName.StartsWith(CodeFixSourceAttributeFullName) ?? false);
        if (attr is null) return null;

        var codeFixType = attr.AttributeClass!.TypeArguments[0].FullyQualifiedName;

        if (!attr.TryGetNamedArgumentValue("TestName", out string? testName))
            testName = null;

        if (!IsValidMethodName(testName))
            testName = null; // To avoid creating method with invalid name, we set it to null and use the field name instead.

        var location = field.Locations.FirstOrDefault();
        var filePath = location?.SourceTree?.FilePath ?? "";

        return new(field.Name, code, codeFixType, testName, filePath);
    } // internal static CodeFixSourceInfo? FromFieldSymbol (IFieldSymbol)

    private static bool IsValidMethodName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (!SyntaxFacts.IsValidIdentifier(name)) return false;

        if (SyntaxFacts.IsReservedKeyword(SyntaxFacts.GetKeywordKind(name))) return false;
        if (SyntaxFacts.IsContextualKeyword(SyntaxFacts.GetContextualKeywordKind(name))) return false;

        return true;
    } // private static bool IsValidMethodName (string?)

    internal string GetTestMethodName()
        => this.TestName ?? ToPascalCase(this.SourceName);

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var buffer = new char[name.Length];
        var writeIndex = 0;

        var capitalizeNext = true;

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (c == '_')
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                buffer[writeIndex++] = char.ToUpperInvariant(c);
                capitalizeNext = false;
            }
            else
            {
                buffer[writeIndex++] = c;
            }
        }

        return new(buffer, 0, writeIndex);
    } // private static string ToPascalCase (string)
} // internal record CodeFixSourceInfo (string, string, string, string?, string)
