using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Assertions.Collections.Conditions;

public class EnumerableEquivalentToExpectedValueAssertCondition<TActual, TInner>(
    IEnumerable<TInner> expected,
    IEqualityComparer<TInner?>? equalityComparer,
    CollectionOrdering collectionOrdering)
    : ExpectedValueAssertCondition<TActual, IEnumerable<TInner>>(expected)
    where TActual : IEnumerable<TInner>?
{
    protected override string GetExpectation() => $"to be equivalent to {(expected != null ? string.Join(',', expected) : null)}";

    protected override AssertionResult GetResult(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return AssertionResult.Passed;
        }

        IEnumerable<TInner>? orderedActual;
        if (collectionOrdering == CollectionOrdering.Any)
        {
            orderedActual = actualValue?.Order();
            expectedValue = expectedValue?.Order();
        }
        else
        {
            orderedActual = actualValue;
        }

        return AssertionResult
            .FailIf(
                () => orderedActual is null,
                () => "it is null")
            .OrFailIf(
                () => expectedValue is null,
                () => "it is not null")
            .OrFailIf(
                () => !orderedActual!.SequenceEqual(expectedValue!, equalityComparer),
                () => $"it is {string.Join(',', orderedActual!)}"
            );
    }
}