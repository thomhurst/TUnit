using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Strings.Conditions;

public class StringNotContainsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override string GetExpectation()
        => $"to not contain {Formatter.Format(expected).TruncateWithEllipsis(100)}";

    protected override AssertionResult GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is null,
                    () => "it was null");
        }

        return AssertionResult
            .FailIf(
                () => actualValue.Contains(expectedValue!, stringComparison),
                () => $"it was found in {Formatter.Format(ActualValue).TruncateWithEllipsis(100)}");
    }
}