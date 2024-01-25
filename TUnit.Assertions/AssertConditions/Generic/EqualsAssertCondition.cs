namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<T> : AssertCondition<T>
{
    private readonly T _expected;

    public EqualsAssertCondition(T expected) : base(expected)
    {
        _expected = expected;
    }
    
    public override bool Matches(T actualValue)
    {
        Message = $"Expected {_expected} but received {actualValue}";
        return Equals(actualValue, _expected);
    }
    
    public override string Message { get; protected set; } = string.Empty;
}