namespace TUnit.Assertions.AssertConditions.String;

public class StringNotEqualsAssertCondition : AssertCondition<string, string>
{
    private readonly StringComparison _stringComparison;
    
    public StringNotEqualsAssertCondition(string expected, StringComparison stringComparison) : base(expected)
    {
        _stringComparison = stringComparison;
    }
    
    private protected override bool Passes(string? actualValue, Exception? exception)
    {
        return !string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                              "{ActualValue}" is equal to "{ExpectedValue}"
                                              """;
}