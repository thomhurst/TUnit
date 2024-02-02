namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual, TExpected>(AssertionBuilder<TActual> assertionBuilder, TExpected expected)
    : AssertCondition<TActual, TExpected>(assertionBuilder, expected)
{
    protected override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return Equals(actualValue, ExpectedValue);
    }
}