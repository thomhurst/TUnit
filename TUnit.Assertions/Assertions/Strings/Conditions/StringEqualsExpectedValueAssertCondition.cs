using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override string GetExpectation()
        => $"to be equal to {Formatter.Format(expected).TruncateWithEllipsis(100)}";

    protected override AssertionResult GetResult(string? actualValue, string? expectedValue)
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
                $"found {Formatter.Format(ActualValue).TruncateWithEllipsis(100)} which {new StringDifference(actualValue, expectedValue)}");
    }
}