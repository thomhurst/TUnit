namespace TUnit.Assertions.AssertConditions.String;

public class StringContainsAssertCondition(string expected, StringComparison stringComparison)
    : AssertCondition<string, string>(expected)
{
    private protected override bool Passes(string? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            OverriddenMessage = $"{ActualExpression ?? "Actual string"} is null";
            return false;
        }
        
        if (ExpectedValue is null)
        {
            OverriddenMessage = "No expected value given";
            return false;
        }
        
        return actualValue.Contains(ExpectedValue, stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                              Expected "{ActualValue}" to contain "{ExpectedValue}"
                                              """;
}