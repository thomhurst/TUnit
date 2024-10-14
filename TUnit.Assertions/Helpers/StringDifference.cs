using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Helpers;

internal class StringDifference(string? actualValue, string? expectedValue, IEqualityComparer<string>? comparer = null)
{
    private const char ArrowDown = '\u2193';
    private const char ArrowUp = '\u2191';

    private readonly IEqualityComparer<string> _comparer = comparer ?? StringComparer.Ordinal;

    /// <summary>
    /// Returns the first index at which the two values do not match.
    /// </summary>
    public int IndexOfFirstMismatch()
    {
        if (actualValue == expectedValue)
        {
            return -1;
        }

        if (actualValue is null || expectedValue is null)
        {
            return 0;
        }

        return IndexOfFirstMismatch(actualValue, expectedValue, _comparer);
    }

    private static int IndexOfFirstMismatch(string actualValue, string expectedValue, IEqualityComparer<string> comparer)
    {
        for (var index = 0; index < Math.Max(actualValue.Length, expectedValue.Length); index++)
        {
            var actualChar = actualValue.ElementAtOrDefault(index).ToString();
            var expectedChar = expectedValue.ElementAtOrDefault(index).ToString();
            if (index >= expectedValue.Length || !comparer.Equals(actualChar, expectedChar))
            {
                return index;
            }
        }

        if (actualValue.Length < expectedValue.Length)
        {
            return actualValue.Length;
        }

        return -1;
    }

    public override string ToString()
    {
        return ToString("differs at index");
    }

    /// <summary>
    /// Writes a string representation of the difference, starting with the <paramref name="prefix"/>.
    /// </summary>
    /// <param name="prefix">The prefix, e.g. <c>differs at index</c></param>
    /// <returns></returns>
    public string ToString(string prefix)
    {
        var initialIndexOfDifference = IndexOfFirstMismatch();

        var startIndex = Math.Max(0, initialIndexOfDifference - 25);

        var actualLine = actualValue
            ?.Substring(startIndex, Math.Min(actualValue.Length - startIndex, 55))
            .ShowNewLines()
            .Trim()
            .TruncateWithEllipsis(50) ?? string.Empty;

        var expectedLine = expectedValue
            ?.Substring(startIndex, Math.Min(expectedValue.Length - startIndex, 55))
            .ShowNewLines()
            .Trim()
            .TruncateWithEllipsis(50) ?? string.Empty;

        var spacesBeforeArrow = IndexOfFirstMismatch(actualLine, expectedLine, _comparer) + 1;

        return $"""
                {prefix} {initialIndexOfDifference}:
                   {new string(' ', spacesBeforeArrow)}{ArrowDown}
                   "{actualLine}"
                   "{expectedLine}"
                   {new string(' ', spacesBeforeArrow)}{ArrowUp}
                """;
    }
}