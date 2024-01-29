namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual, TExpected>(
    IReadOnlyCollection<AssertCondition<TActual, TExpected>> previousConditions,
    TExpected expected)
    : AssertCondition<TActual, TExpected>(previousConditions, expected)
{
    public override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected override bool Passes(TActual actualValue)
    {
        return Equals(actualValue, ExpectedValue);
    }
}