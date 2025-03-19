using TUnit.Assertions.Enums;
using TUnit.Assertions.Equality;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToExpectedValueAssertCondition<TActual, TInner>(
    IEnumerable<TInner>? expected,
    IEqualityComparer<TInner?> equalityComparer,
    CollectionOrdering collectionOrdering)
    : ExpectedValueAssertCondition<TActual, IEnumerable<TInner>>(expected)
    where TActual : IEnumerable<TInner>?
{
    protected override string GetExpectation()
    {
        if (!typeof(TInner).IsSimpleType()
            && equalityComparer is EquivalentToEqualityComparer<TInner> { ComparisonFailures.Length: > 0 })
        {
            return "to match";
        }
        
        return $"to be equivalent to {(expected != null ? Formatter.Format(expected) : null)}";
    }

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return AssertionResult.Passed;
        }

        var enumeratedActual = actualValue?.ToArray();
        var enumeratedExpected = expectedValue?.ToArray();
        
        return AssertionResult
            .FailIf(enumeratedActual is null,
                "it is null")
            .OrFailIf(enumeratedExpected is null,
                "it is not null")
            .OrFailIf(collectionOrdering == CollectionOrdering.Matching && !enumeratedActual!.SequenceEqual(enumeratedExpected!, equalityComparer), FailureMessage(enumeratedActual))
            .OrFailIf(collectionOrdering == CollectionOrdering.Any && !EqualsAnyOrder(enumeratedActual!, enumeratedExpected!, equalityComparer), FailureMessage(enumeratedActual));
    }

    private static bool EqualsAnyOrder(TInner[] actualValue, TInner[] expectedValue,
        IEqualityComparer<TInner?> equalityComparer)
    {
        return actualValue.Length == expectedValue.Length && !actualValue.Except(expectedValue, equalityComparer).Any(); 
    }

    private string FailureMessage(IEnumerable<TInner>? orderedActual)
    {
        if (!typeof(TInner).IsSimpleType()
            && equalityComparer is EquivalentToEqualityComparer<TInner> { ComparisonFailures.Length: > 0 } equivalentToEqualityComparer)
        {
            return equivalentToEqualityComparer.GetFailureMessages();
        }
        
        return $"it is {string.Join(",", Formatter.Format(orderedActual!))}";
    }
}