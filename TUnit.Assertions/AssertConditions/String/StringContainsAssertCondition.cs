namespace TUnit.Assertions.AssertConditions.String;

public class StringContainsAssertCondition : AssertCondition<string, string>
{
    private readonly StringComparison _stringComparison;
    
    public StringContainsAssertCondition(AssertionBuilder<string> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected internal override bool Passes(string? actualValue, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(actualValue);
        ArgumentNullException.ThrowIfNull(ExpectedValue);
        
        return actualValue.Contains(ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              Expected "{ExpectedValue}" but received "{ActualValue}"
                                              """;
}