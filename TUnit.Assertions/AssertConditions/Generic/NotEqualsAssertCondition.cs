﻿using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Generic;

public class NotEqualsAssertCondition<TActual, TAnd, TOr>(AssertionBuilder<TActual> assertionBuilder, TActual expected)
    : AssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder, expected)
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected override string DefaultMessage => $"{ActualValue} equals {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return !Equals(actualValue, ExpectedValue);
    }
}