namespace TUnit.Assertions.AssertConditions.Generic;

public class NotEqualsExpectedValueAssertCondition<TActual>(TActual expected)
    : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
    protected override string GetFailureMessage(TActual? actualValue, TActual? expectedValue) => $"{ActualValue} equals {ExpectedValue}";

    protected override bool Passes(TActual? actualValue, TActual? expectedValue)
    {
        return !Equals(actualValue, ExpectedValue);
    }
}