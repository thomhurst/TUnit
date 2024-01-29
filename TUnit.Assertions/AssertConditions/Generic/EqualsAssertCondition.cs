namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual, TExpected>(
    IReadOnlyCollection<ExpectedValueAssertCondition<TActual, TExpected>> previousConditions,
    TExpected expected)
    : ExpectedValueAssertCondition<TActual, TExpected>(previousConditions, expected)
{
    public override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected override bool Passes(TActual actualValue)
    {
        return Equals(actualValue, ExpectedValue);
    }
}