namespace TUnit.Assertions.AssertConditions.Generic;

public class NotTypeOfExpectedValueAssertCondition<TActual, TExpected>
    : BaseAssertCondition<TActual>
{
    protected internal override string GetFailureMessage() => $"{ActualValue} is {typeof(TExpected).Name}";

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue?.GetType() != typeof(TExpected);
    }
}