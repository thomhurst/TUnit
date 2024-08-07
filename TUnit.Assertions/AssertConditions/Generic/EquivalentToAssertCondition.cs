using System.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Generic;

public class EquivalentToAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public EquivalentToAssertCondition(AssertionBuilder<TActual> assertionBuilder, TActual expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => $"""
                                                The two items were not equivalent
                                                   Actual: {ActualValue}
                                                   Expected: {ExpectedValue}
                                                """;

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return true;
        }

        if (actualValue is null || ExpectedValue is null)
        {
            return false;
        }

        if (actualValue is IEqualityComparer<TActual> equalityComparerActual)
        {
            return equalityComparerActual.Equals(actualValue, ExpectedValue);
        }
        
        if (actualValue is IEqualityComparer equalityComparerActual2)
        {
            return equalityComparerActual2.Equals(actualValue, ExpectedValue);
        }
        
        if (actualValue is IComparable<TActual> comparableActual)
        {
            return comparableActual.CompareTo(ExpectedValue) == 0;
        }
        
        if (actualValue is IComparable comparableActual2)
        {
            return comparableActual2.CompareTo(ExpectedValue) == 0;
        }
        
        if (ExpectedValue is IEqualityComparer<TActual> equalityComparerActual3)
        {
            return equalityComparerActual3.Equals(actualValue, ExpectedValue);
        }
        
        if (ExpectedValue is IEqualityComparer equalityComparerActual4)
        {
            return equalityComparerActual4.Equals(actualValue, ExpectedValue);
        }
        
        if (ExpectedValue is IComparable<TActual> comparableActual3)
        {
            return comparableActual3.CompareTo(actualValue) == 0;
        }
        
        if (ExpectedValue is IComparable comparableActual4)
        {
            return comparableActual4.CompareTo(actualValue) == 0;
        }

        return actualValue.Equals(ExpectedValue);
    }
}