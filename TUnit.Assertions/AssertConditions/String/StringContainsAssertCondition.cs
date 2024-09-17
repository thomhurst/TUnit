namespace TUnit.Assertions.AssertConditions.String;

public class StringContainsAssertCondition(string expected, StringComparison stringComparison)
    : AssertCondition<string, string>(expected)
{
    private protected override bool Passes(string? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            OverriddenMessage = "Actual string is null";
            return false;
        }
        
        if (ExpectedValue is null)
        {
            OverriddenMessage = "Expected string is null";
            return false;
        }
        
        return actualValue.Contains(ExpectedValue, stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                              Expected "{ActualValue}" to contain "{ExpectedValue}"
                                              """;
}