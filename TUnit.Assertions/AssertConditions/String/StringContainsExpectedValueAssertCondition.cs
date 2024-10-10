using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.String;

public class StringContainsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override string GetExpectation()
        => $"to contain {Format(expected).TruncateWithEllipsis(100)}";

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
                () => !actualValue.Contains(expectedValue, stringComparison),
                $"it was not found in {Format(actualValue).TruncateWithEllipsis(100)}");
    }
}