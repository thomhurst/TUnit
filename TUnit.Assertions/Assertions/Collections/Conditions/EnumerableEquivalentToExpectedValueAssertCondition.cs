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

    protected override Task<AssertionResult> GetResult(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return AssertionResult.Passed;
        }

        TInner[]? orderedActual;
        if (collectionOrdering == CollectionOrdering.Any)
        {
            orderedActual = actualValue?.OrderBy(x => x, new ComparerWrapper<TInner>(equalityComparer)).ToArray();
            expectedValue = expectedValue?.OrderBy(x => x, new ComparerWrapper<TInner>(equalityComparer));
        }
        else
        {
            orderedActual = actualValue?.ToArray();
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
            if (equalityComparer is IComparer<T> comparer)
            {
                return comparer.Compare(x!, y!);
            }
            
            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null || y is null)
            {
                return -1;
            }

            if (equalityComparer.Equals(x, y))
            {
                return 0;
            }
            
            return Comparer<T>.Default.Compare(x, y);
        }
    }
}