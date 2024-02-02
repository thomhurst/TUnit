namespace TUnit.Assertions.AssertConditions.Generic;

public class TypeOfAssertCondition<TActual, TExpected>(AssertionBuilder<TActual> assertionBuilder)
    : AssertCondition<TActual, TExpected>(assertionBuilder, default)
{
    protected override string DefaultMessage => $"{ActualValue} is {ActualValue?.GetType().Name ?? "null"} instead of {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue?.GetType() == typeof(TExpected);
    }
}