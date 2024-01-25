namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition : AssertCondition<string>
{
    private readonly StringComparison _stringComparison;

    public StringEqualsAssertCondition(string expected, StringComparison stringComparison) : base(expected)
    {
        _stringComparison = stringComparison;
    }
    
    public override bool Matches(string actualValue)
    {
        Message = $"Expected {ExpectedValue} but received {actualValue}";
        return string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    public override string Message { get; protected set; } = string.Empty;
}