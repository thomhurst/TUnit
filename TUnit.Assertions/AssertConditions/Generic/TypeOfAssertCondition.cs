namespace TUnit.Assertions.AssertConditions.Generic;

public class TypeOfAssertCondition<TActual, TExpected>()
    : AssertCondition<TActual, TExpected>(default)
{
    protected override string DefaultMessage => $"{ActualValue} is {ActualValue?.GetType().Name ?? "null"} instead of {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue)
    {
        return actualValue?.GetType() == typeof(TExpected);
    }
}