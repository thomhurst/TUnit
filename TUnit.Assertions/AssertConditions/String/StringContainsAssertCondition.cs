namespace TUnit.Assertions.AssertConditions.String;

public class StringContainsAssertCondition(string expected, StringComparison stringComparison)
    : AssertCondition<string, string>(expected)
{
    protected override bool Passes(string? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            return FailWithMessage($"{ActualExpression ?? "Actual string"} is null");
        }
        
        if (ExpectedValue is null)
        {
            return FailWithMessage("No expected value given"); ;
        }
        
        return actualValue.Contains(ExpectedValue, stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                              Expected "{ActualValue}" to contain "{ExpectedValue}"
                                              """;
}