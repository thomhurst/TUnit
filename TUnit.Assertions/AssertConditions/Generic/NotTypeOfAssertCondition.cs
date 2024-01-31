namespace TUnit.Assertions.AssertConditions.Generic;

public class NotTypeOfAssertCondition<TActual, TExpected> : TypeOfAssertCondition<TActual, TExpected>
{
    protected override string DefaultMessage => $"{ActualValue} is {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue)
    {
        return !base.Passes(actualValue);
    }
}