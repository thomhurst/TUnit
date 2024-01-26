namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<T> : AssertCondition<T>
{
    private readonly T _expected;

    public EqualsAssertCondition(T expected) : base(expected)
    {
        _expected = expected;
    }

    internal override Func<(T ExpectedValue, T ActualValue), string> MessageFactory { get; set; }
    = tuple => $"Expected {tuple.ExpectedValue} but received {tuple.ActualValue}";

    protected override bool Passes(T actualValue)
    {
        return Equals(actualValue, _expected);
    }
}