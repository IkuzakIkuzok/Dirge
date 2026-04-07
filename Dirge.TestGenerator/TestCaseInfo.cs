
// (c) 2026 Kazuki Kohzuki

using Dirge.Utils;

namespace Dirge.TestGenerator;

internal record TestCaseInfo(string SourceName, bool SourceIsStatic, string? TestName, string FilePath)
{
    private const string TestSourceAttributeFullName = $"{TestSourceAttributeGenerator.Namespace}.{TestSourceAttributeGenerator.TestSourceAttribute}";

    internal static TestCaseInfo? FromFieldSymbol(IFieldSymbol field)
    {
        if (field.Type.SpecialType != SpecialType.System_String) return null;

        var attr = field.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.FullName == TestSourceAttributeFullName);
        if (attr is null) return null;

        if (!attr.TryGetNamedArgumentValue("TestName", out string? testName))
            testName = null;

        if (!IsValidMethodName(testName))
            testName = null; // To avoid creating method with invalid name, we set it to null and use the field name instead.

        var location = field.Locations.FirstOrDefault();
        var filePath = location?.SourceTree?.FilePath ?? "";

        return new(field.Name, field.IsStatic, testName, filePath);
    } // internal static TestCaseInfo? FromFieldSymbol (IFieldSymbol)

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
} // private record FieldInfo (string, bool, string?)
