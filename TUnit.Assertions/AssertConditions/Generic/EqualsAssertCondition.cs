using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual, TAnd, TOr>(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TActual expected)
    : AssertCondition<TActual, TActual, TAnd, TOr>(expected)
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected override string DefaultMessage => $"""
                                                 Expected: {ExpectedValue}
                                                 Received: {ActualValue}
                                                 """;

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        if (actualValue is IEquatable<TActual> equatable)
        {
            return equatable.Equals(ExpectedValue);
        }
        
        return Equals(actualValue, ExpectedValue);
    }
}