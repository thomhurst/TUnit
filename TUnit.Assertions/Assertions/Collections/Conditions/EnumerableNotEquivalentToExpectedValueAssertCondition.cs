using TUnit.Assertions.Enums;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotEquivalentToExpectedValueAssertCondition<TActual, TInner>(
    IEnumerable<TInner>? expected,
    IEqualityComparer<TInner?> equalityComparer,
    CollectionOrdering collectionOrdering)
    : ExpectedValueAssertCondition<TActual, IEnumerable<TInner>>(expected)
    where TActual : IEnumerable<TInner>?
{
    protected override string GetExpectation() => $"to not be equivalent to {(ExpectedValue != null ? Formatter.Format(ExpectedValue) : null)}";

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null != expectedValue is null)
        {
            return AssertionResult.Passed;
        }

        var enumeratedActual = actualValue?.ToArray();
        var enumeratedExpected = expectedValue?.ToArray();
        
        return AssertionResult
            .FailIf(actualValue is null && expectedValue is null,
                "it is null")
            .OrFailIf(collectionOrdering == CollectionOrdering.Matching && enumeratedActual!.SequenceEqual(enumeratedExpected!, equalityComparer), "the two Enumerables were equivalent")
            .OrFailIf(collectionOrdering == CollectionOrdering.Any && EqualsAnyOrder(enumeratedActual!, enumeratedExpected!, equalityComparer), "the two Enumerables were equivalent");
    }

    private static bool EqualsAnyOrder(TInner[] actualValue, TInner[] expectedValue,
        IEqualityComparer<TInner?> equalityComparer)
    {
        return actualValue.Length == expectedValue.Length && !actualValue.Except(expectedValue, equalityComparer).Any(); 
    }
}