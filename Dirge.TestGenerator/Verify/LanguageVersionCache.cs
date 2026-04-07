
// (c) 2026 Kazuki Kohzuki

namespace Dirge.TestGenerator.Verify;

internal static class LanguageVersionCache
{
    private static readonly HashSet<int> _validValues = [.. Enumerable.Cast<int>(Enum.GetValues(typeof(LanguageVersion)))];

    internal static IEnumerable<int> ValidValues => _validValues;

    internal static bool IsValid(int value) => _validValues.Contains(value);
} // internal static class LanguageVersionCache
