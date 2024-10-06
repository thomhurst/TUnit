namespace TUnit.Assertions.AssertConditions.String;

public class StringNotEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override bool Passes(string? actualValue, string? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return true;
        }

        if (actualValue is null || expectedValue is null)
        {
            return false;
        }
        
        return !actualValue.Equals(expectedValue, stringComparison);
    }

    protected override string GetFailureMessage(string? actualValue, string? expectedValue) => $"""
                                              {Format(ActualValue)} is equal to {Format(ExpectedValue)}
                                              """;
}