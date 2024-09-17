using System.Collections;

namespace TUnit.Assertions.AssertConditions.Generic;

public class EquivalentToAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public EquivalentToAssertCondition(TActual expected) : base(expected)
    {
    }

    protected override string DefaultMessage => $"""
                                                The two items were not equivalent
                                                   Actual: {ActualValue}
                                                   Expected: {ExpectedValue}
                                                """;

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
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

        if (actualValue is IEnumerable enumerable && ExpectedValue is IEnumerable enumerable2)
        {
            return enumerable.Cast<object>().SequenceEqual(enumerable2.Cast<object>());
        }

        return actualValue.Equals(ExpectedValue);
    }
}