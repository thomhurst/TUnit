using System.Text;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Equality;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToExpectedValueAssertCondition<TActual, TInner>(
    IEnumerable<TInner> expected,
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

    protected override AssertionResult GetResult(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return AssertionResult.Passed;
        }

        IEnumerable<TInner>? orderedActual;
        if (collectionOrdering == CollectionOrdering.Any)
        {
            orderedActual = actualValue?.OrderBy(x => x, new ComparerWrapper<TInner>(equalityComparer));
            expectedValue = expectedValue?.OrderBy(x => x, new ComparerWrapper<TInner>(equalityComparer));
        }
        else
        {
            orderedActual = actualValue;
        }

        return AssertionResult
            .FailIf(orderedActual is null,
                "it is null")
            .OrFailIf(expectedValue is null,
                "it is not null")
            .OrFailIf(!orderedActual!.SequenceEqual(expectedValue!, equalityComparer), FailureMessage(orderedActual)
            );
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
    
    internal class ComparerWrapper<T>(IEqualityComparer<T> equalityComparer) : IComparer<T>
    {
        public int Compare(T? x, T? y)
        {
            return equalityComparer.Equals(x, y) ? 0 : -1;
        }
    }
}