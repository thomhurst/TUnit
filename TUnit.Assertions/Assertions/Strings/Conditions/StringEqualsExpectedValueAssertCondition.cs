using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override string GetExpectation()
        => $"to be equal to {Formatter.Format(expected).TruncateWithEllipsis(100)}";

    protected override ValueTask<AssertionResult> GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(expectedValue is not null,
                    "it was null");
        }

        return AssertionResult
            .FailIf(!string.Equals(actualValue, expectedValue, stringComparison),
                $"found {Formatter.Format(ActualValue).TruncateWithEllipsis(100)} which {new StringDifference(actualValue, expectedValue)}");
    }

    public StringEqualsExpectedValueAssertCondition WithTrimming()
    {
        WithTransform(s => s?.Trim(), s => s?.Trim());
        return this;
    }

    public StringEqualsExpectedValueAssertCondition WithNullAndEmptyEquality()
    {
        WithComparer((actual, expected) =>
        {
            if (actual == null && expected == string.Empty)
            {
                return AssertionDecision.Pass;
            }

            if (expected == null && actual == string.Empty)
            {
                return AssertionDecision.Pass;
            }

            return AssertionDecision.Continue;
        });
        
        return this;
    }

    public StringEqualsExpectedValueAssertCondition IgnoringWhitespace()
    {
        WithTransform(StringUtils.StripWhitespace, StringUtils.StripWhitespace);
        return this;
    }
}