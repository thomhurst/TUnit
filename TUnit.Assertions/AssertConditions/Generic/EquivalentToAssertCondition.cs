using System.Collections;

namespace TUnit.Assertions.AssertConditions.Generic;

public class EquivalentToAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public EquivalentToAssertCondition(TActual expected) : base(expected)
    {
    }

    protected internal override string GetFailureMessage() => $"""
                                                The two items were not equivalent
                                                   Actual: {ActualValue}
                                                   Expected: {ExpectedValue}
                                                """;

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return true;
        }

        if (actualValue is null || ExpectedValue is null)
        {
            return false;
        }

        if (actualValue is IEqualityComparer<TActual> typedEqualityComparer)
        {
            return typedEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        
        if (actualValue is IEqualityComparer basicEqualityComparer)
        {
            return basicEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        
        if (ExpectedValue is IEqualityComparer<TActual> expectedTypeEqualityComparer)
        {
            return expectedTypeEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        
        if (ExpectedValue is IEqualityComparer expectedBasicEqualityComparer)
        {
            return expectedBasicEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        
        if (actualValue is IEnumerable enumerable && ExpectedValue is IEnumerable enumerable2)
        {
            return enumerable.Cast<object>().SequenceEqual(enumerable2.Cast<object>());
        }

        return actualValue.Equals(ExpectedValue);
    }
}