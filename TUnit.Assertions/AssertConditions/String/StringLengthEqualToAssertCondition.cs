namespace TUnit.Assertions.AssertConditions.String;

public class StringLengthEqualToAssertCondition : AssertCondition<string, int>
{
    public StringLengthEqualToAssertCondition(AssertionBuilder<string> assertionBuilder, int expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => $"Length is {GetCount(ActualValue)} instead of {ExpectedValue}";
    
    protected internal override bool Passes(string? actualValue, Exception? exception)
    {
        return GetCount(actualValue) == ExpectedValue;
    }

    private int GetCount(string? actualValue)
    {
        ArgumentNullException.ThrowIfNull(actualValue);

        return actualValue.Length;
    }
}