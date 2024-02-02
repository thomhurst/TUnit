namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition : AssertCondition<string, string>
{
    private readonly StringComparison _stringComparison;
    
    public StringEqualsAssertCondition(AssertionBuilder<string> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected internal override bool Passes(string? actualValue, Exception? exception)
    {
        return string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              Expected "{ExpectedValue}" but received "{ActualValue}"
                                              """;
}