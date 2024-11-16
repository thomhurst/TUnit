using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Strings.Conditions;

public class StringContainsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    internal bool IgnoreWhitespace { get; set; }
    
    protected override string GetExpectation()
        => $"to contain {Formatter.Format(expected).TruncateWithEllipsis(100)}";

    protected override AssertionResult GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is not null,
                    () => "it was null");
        }

        return AssertionResult
            .FailIf(
                () => !actualValue.Contains(expectedValue!, stringComparison),
                () => $"it was not found. {MessageSuffix()}");
    }

    private string MessageSuffix()
    {
        if (ExpectedValue?.Length > 1000)
        {
            return string.Empty;
        }

        var closestSubstring = StringUtils.FindClosestSubstring(ActualValue!, ExpectedValue, stringComparison, IgnoreWhitespace, out var differIndexOnActual, out var differIndexOnExpected);

        var startIndex = differIndexOnExpected + 25 > ExpectedValue?.Length 
            ? Math.Max(ExpectedValue.Length - 46, 0)
            : Math.Max(differIndexOnExpected - 25, 0);
        
        var expectedValue = ExpectedValue?.Substring(startIndex, Math.Min(ExpectedValue.Length - startIndex, 50));
        
        return $"Found a closest match which {new StringDifference(closestSubstring, expectedValue)
        {
            OverriddenIndex = differIndexOnActual
        }}";
    }
}