using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableDistinctItemsExpectedValueAssertCondition<TActual, TInner>(IEqualityComparer<TInner?>? equalityComparer)
    : BaseAssertCondition<TActual>
    where TActual : IEnumerable
{
    protected override string GetExpectation() => "items to be distinct";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            return AssertionResult.Fail($"{ActualExpression ?? typeof(TActual).Name} is null");
        }

        var list = actualValue.Cast<TInner>().ToList();

        var distinct = list.Distinct(equalityComparer);

        return AssertionResult
            .FailIf(
                () => list.Count != distinct.Count(),
                "duplicate items found in the collection");

    }
}