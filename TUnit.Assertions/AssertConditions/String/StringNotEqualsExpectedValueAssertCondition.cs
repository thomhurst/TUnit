using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.String;

public class StringNotEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override string GetExpectation()
        => $"to not be equal to {Format(expected).TruncateWithEllipsis(100)}";

    protected override AssertionResult GetResult(string? actualValue, string? expectedValue)
    {

        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is null,
                    "it was null");
        }

        return AssertionResult
            .FailIf(
                () => string.Equals(actualValue, expectedValue, stringComparison),
                $"it was");
    }
}