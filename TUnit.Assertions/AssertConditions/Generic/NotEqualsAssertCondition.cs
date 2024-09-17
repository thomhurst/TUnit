namespace TUnit.Assertions.AssertConditions.Generic;

public class NotEqualsAssertCondition<TActual>(TActual expected)
    : AssertCondition<TActual, TActual>(expected)
{
    protected override string DefaultMessage => $"{ActualValue} equals {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        return !Equals(actualValue, ExpectedValue);
    }
}