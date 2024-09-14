using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Generic;

public class NotEqualsAssertCondition<TActual, TAnd, TOr>(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TActual expected)
    : AssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder, expected)
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected override string DefaultMessage => $"{ActualValue} equals {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return !Equals(actualValue, ExpectedValue);
    }
}