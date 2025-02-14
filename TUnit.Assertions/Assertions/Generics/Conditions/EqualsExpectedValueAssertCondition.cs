#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#endif
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class EqualsExpectedValueAssertCondition<TActual>(TActual expected, IEqualityComparer<TActual> equalityComparer) : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
    public EqualsExpectedValueAssertCondition(TActual expected) : this(expected, EqualityComparer<TActual>.Default)
    {
    }
    
    protected override string GetExpectation()
        => $"to be equal to {expected}";

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, TActual? expectedValue)
    {
        if (equalityComparer.Equals(actualValue, expectedValue))
        {
            return AssertionResult.Passed;
        }
        
        if (actualValue is IEquatable<TActual> equatable)
        {
            return AssertionResult
                .FailIf(!equatable.Equals(expected),
                    $"found {actualValue}");
        }

        return AssertionResult
            .FailIf(!Equals(actualValue, expectedValue),
                $"found {actualValue}");
    }
}