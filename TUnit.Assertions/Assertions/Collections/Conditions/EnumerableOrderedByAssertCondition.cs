using TUnit.Assertions.Enums;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableOrderedByAssertCondition<TActual, TInner, TComparisonItem>(
    IComparer<TComparisonItem?> comparer,
    Func<TInner, TComparisonItem> comparisonItemSelector,
    Order order)
    : BaseAssertCondition<TActual> where TActual : IEnumerable<TInner>
{
    internal protected override string GetExpectation()
    {
        return $"to be in {order} order";
    }

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        if (actualValue is null)
        {
            return AssertionResult.Fail("the enumerable was null");
        }

        var enumerated = actualValue.ToArray();

        var isOrdered = order == Order.Ascending
            ? enumerated.SequenceEqual(enumerated.OrderBy(comparisonItemSelector, comparer))
            : enumerated.SequenceEqual(enumerated.OrderByDescending(comparisonItemSelector, comparer));

        return AssertionResult.FailIf(!isOrdered, "the enumerable was not in the expected order");
    }
}
