using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageEqualToExpectedValueAssertCondition<TActual>(
    string expectedMessage,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => $"to have Message equal to \"{expectedMessage}\"";

    protected internal override AssertionResult GetResult(TActual? actualValue, Exception? exception)
    {
        var actualException = exceptionSelector(exception);

        return AssertionResult
            .FailIf(
                () => actualException is null,
                "the exception is null")
            .OrFailIf(
                () => !string.Equals(actualException.Message, expectedMessage, stringComparison),
                $"it differs at {GetLocation(actualException.Message, expectedMessage)}");
    }

    private string GetLocation(string? actualValue, string? expectedValue)
    {
        const char arrowDown = '\u2193';
        const char arrowUp = '\u2191';
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
                   {new string(' ', spacesBeforeArrow)}{arrowDown}
                   {Format(actualLine)}
                   {Format(expectedLine)}
                   {new string(' ', spacesBeforeArrow)}{arrowUp}
                """;
    }
}