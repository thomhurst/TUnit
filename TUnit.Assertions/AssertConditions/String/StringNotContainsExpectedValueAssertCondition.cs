namespace TUnit.Assertions.AssertConditions.String;

public class StringNotContainsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override AssertionResult Passes(string? actualValue, string? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return true;
        }

        if (actualValue is null || expectedValue is null)
        {
            return false;
        }
        
        return !actualValue.Contains(expectedValue, stringComparison);
    }

    protected override string GetFailureMessage(string? actualValue, string? expectedValue) => $"""
                                              Expected {Format(ActualValue)} to not contain {Format(ExpectedValue)}
                                              """;
}