namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual>(AssertionBuilder<TActual> assertionBuilder, TActual expected)
    : AssertCondition<TActual, TActual>(assertionBuilder, expected)
{
    protected override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return Equals(actualValue, ExpectedValue);
    }
}