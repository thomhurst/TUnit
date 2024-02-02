namespace TUnit.Assertions.AssertConditions.Generic;

public class NotTypeOfAssertCondition<TActual, TExpected>(AssertionBuilder<TActual> assertionBuilder) : TypeOfAssertCondition<TActual, TExpected>(assertionBuilder)
{
    protected override string DefaultMessage => $"{ActualValue} is {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return !base.Passes(actualValue, exception);
    }
}