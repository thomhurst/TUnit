using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override bool Passes(string? actualValue, string? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return true;
        }

        if (actualValue is null || expectedValue is null)
        {
            return false;
        }
        
        return string.Equals(actualValue, expectedValue, stringComparison);
    }
    
    protected override string GetFailureMessage(string? actualValue, string? expectedValue)
    {
        if (actualValue?.Length <= 100 && expectedValue?.Length <= 100)
        {
            return $"""
                    Expected: {Format(ExpectedValue)}
                    Received: {Format(ActualValue)}
                    {GetLocation(actualValue, expectedValue)}
                    """;
        }
        
        return GetLocation(actualValue, expectedValue);
    }

    private string GetLocation(string? actualValue, string? expectedValue)
    {
        var initialIndexOfDifference = StringUtils.IndexOfDifference(actualValue, expectedValue);

        var startIndex = Math.Max(0, initialIndexOfDifference - 25);
        
        var actualLine = actualValue
            ?.Substring(startIndex, Math.Min(actualValue.Length - startIndex, 55))
            .ReplaceNewLines()
            .Trim()
            .TruncateWithEllipsis(50) ?? string.Empty;

        var expectedLine = expectedValue
            ?.Substring(startIndex, Math.Min(expectedValue.Length - startIndex, 55))
            .ReplaceNewLines()
            .Trim()
            .TruncateWithEllipsis(50) ?? string.Empty;
        
        var spacesBeforeArrow = StringUtils.IndexOfDifference(actualLine, expectedLine) + 1;
        
        return $"""
                Difference at index {initialIndexOfDifference}:
                   {Format(actualLine)}
                   {new string(' ', spacesBeforeArrow)}^
                   {Format(expectedLine)}
                   {new string(' ', spacesBeforeArrow)}^
                """;
    }
}