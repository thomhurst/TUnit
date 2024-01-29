namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual, TExpected>(TExpected expected)
    : AssertCondition<TActual, TExpected>(expected)
{
    public override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected internal override bool Passes(TActual actualValue)
    {
        return Equals(actualValue, ExpectedValue);
    }
}