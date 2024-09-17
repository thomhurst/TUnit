namespace TUnit.Assertions.AssertConditions.Generic;

public class NotTypeOfAssertCondition<TActual, TExpected>()
    : AssertCondition<TActual, TExpected>(default)
{
    protected internal override string GetFailureMessage() => $"{ActualValue} is {typeof(TExpected).Name}";

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue?.GetType() != typeof(TExpected);
    }
}