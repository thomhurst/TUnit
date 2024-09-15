using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Generic;

public class NotTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
    : AssertCondition<TActual, TExpected, TAnd, TOr>(assertionBuilder, default)
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected override string DefaultMessage => $"{ActualValue} is {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue?.GetType() != typeof(TExpected);
    }
}