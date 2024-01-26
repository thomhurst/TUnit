namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual, TExpected> : ExpectedValueAssertCondition<TActual, TExpected>
{

    public EqualsAssertCondition(TExpected expected) : base(expected)
    {
    }

    public override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected override bool Passes(TActual actualValue)
    {
        return Equals(actualValue, ExpectedValue);
    }
}