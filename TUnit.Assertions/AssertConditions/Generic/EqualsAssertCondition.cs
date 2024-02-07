using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual, TAnd, TOr>(AssertionBuilder<TActual> assertionBuilder, TActual expected)
    : AssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder, expected)
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected override string DefaultMessage => $"{GetCallerExpressionPrefix()}Expected {ExpectedValue} but received {ActualValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return Equals(actualValue, ExpectedValue);
    }
}