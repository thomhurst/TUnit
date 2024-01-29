namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition : AssertCondition<string, string>
{
    private readonly StringComparison _stringComparison;

    public StringEqualsAssertCondition(string expected, StringComparison stringComparison) : this([], expected, stringComparison)
    {
    }
    
    public StringEqualsAssertCondition(IReadOnlyCollection<AssertCondition<string, string>> previousConditions, string expected, StringComparison stringComparison) : base(previousConditions, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected override bool Passes(string actualValue)
    {
        return string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    public override string DefaultMessage => $"""
                                              Expected "{ExpectedValue}" but received "{ActualValue}"
                                              """;
}