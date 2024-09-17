namespace TUnit.Assertions.AssertConditions.Generic;

public class NotEqualsAssertCondition<TActual>(TActual expected)
    : AssertCondition<TActual, TActual>(expected)
{
    protected internal override string GetFailureMessage() => $"{ActualValue} equals {ExpectedValue}";

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return !Equals(actualValue, ExpectedValue);
    }
}