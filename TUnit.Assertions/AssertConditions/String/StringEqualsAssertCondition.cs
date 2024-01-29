namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition : AssertCondition<string, string>
{
    private readonly StringComparison _stringComparison;

    public StringEqualsAssertCondition(string expected, StringComparison stringComparison) : this([], null, expected, stringComparison)
    {
    }
    
    public StringEqualsAssertCondition(IReadOnlyCollection<AssertCondition<string, string>> nestedConditions, NestedConditionsOperator? @operator, string expected, StringComparison stringComparison) : base(nestedConditions, @operator, expected)
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