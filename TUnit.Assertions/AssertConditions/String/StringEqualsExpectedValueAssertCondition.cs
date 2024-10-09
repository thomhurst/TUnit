using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
	protected internal override string GetFailureMessage()
		=> $"to be equal to {Format(expected).TruncateWithEllipsis(100)}";

	protected internal override AssertionResult Passes(string? actualValue, string? expectedValue)
	{
		if (actualValue is null)
		{
			return AssertionResult
				.FailIf(
					() => expectedValue is not null,
					"it was null");
		}

		return AssertionResult
			.FailIf(
				() => !string.Equals(actualValue, expectedValue, stringComparison),
				$"found {Format(actualValue).TruncateWithEllipsis(100)} which differs at {GetLocation(actualValue, expectedValue)}");
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
                index {initialIndexOfDifference}:
                   {Format(actualLine)}
                   {new string(' ', spacesBeforeArrow)}^
                   {Format(expectedLine)}
                   {new string(' ', spacesBeforeArrow)}^
                """;
    }
}