namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition : AssertCondition<string>
{
    private readonly StringComparison _stringComparison;

    public StringEqualsAssertCondition(string expected, StringComparison stringComparison) : base(expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected override bool Passes(string actualValue)
    {
        return string.Equals(actualValue, ExpectedValue, _stringComparison);
    }
    
    internal override Func<(string ExpectedValue, string ActualValue), string> MessageFactory { get; set; }
        = tuple => $"""
                    Expected "{tuple.ExpectedValue}" but received "{tuple.ActualValue}"
                    """; 
}