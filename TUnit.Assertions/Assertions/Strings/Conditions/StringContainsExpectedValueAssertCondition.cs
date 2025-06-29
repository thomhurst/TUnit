using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Strings.Conditions;

public class StringContainsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    internal bool IgnoreWhitespace { get; set; }

    internal protected override string GetExpectation()
        => $"to contain {Formatter.Format(ExpectedValue).TruncateWithEllipsis(100)}";

    protected override ValueTask<AssertionResult> GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(expectedValue is not null,
                    "it was null");
        }

        return AssertionResult
            .FailIf(!actualValue.Contains(expectedValue!, stringComparison),
                $"it was not found. {MessageSuffix()}");
    }

    private string MessageSuffix()
    {
        if (ExpectedValue?.Length > 1000)
        {
            return string.Empty;
        }

        var closestSubstring = StringUtils.FindClosestSubstring(ActualValue!, ExpectedValue, stringComparison, IgnoreWhitespace, out var differIndexOnActual, out var differIndexOnExpected);

        var expectedStartDisplayIndex = Math.Max(0, differIndexOnExpected - 25);
        var actualStartDisplayIndex = Math.Max(0, differIndexOnActual - 25);

        var expectedValue = ExpectedValue?.Substring(expectedStartDisplayIndex, Math.Min(ExpectedValue.Length - expectedStartDisplayIndex, 50));
        var actualValue = ActualValue?.Substring(actualStartDisplayIndex, Math.Min(ActualValue.Length - actualStartDisplayIndex, 50));

        return $"Found a closest match which {new StringDifference(closestSubstring, expectedValue)
        {
            OverriddenIndex = differIndexOnActual
        }}";
    }
}
