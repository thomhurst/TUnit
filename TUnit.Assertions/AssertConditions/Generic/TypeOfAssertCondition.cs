using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Generic;

public class TypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder<TActual?> assertionBuilder)
    : AssertCondition<TActual?, TExpected, TAnd, TOr>(assertionBuilder, default)
    where TAnd : And<TActual?, TAnd, TOr>, IAnd<TAnd, TActual?, TAnd, TOr>
    where TOr : Or<TActual?, TAnd, TOr>, IOr<TOr, TActual?, TAnd, TOr>
{
    protected override string DefaultMessage => $"{ActualValue} is {ActualValue?.GetType().Name ?? "null"} instead of {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue?.GetType() == typeof(TExpected);
    }
}