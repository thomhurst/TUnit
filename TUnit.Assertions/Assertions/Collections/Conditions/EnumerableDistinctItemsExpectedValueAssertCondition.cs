namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableDistinctItemsExpectedValueAssertCondition<TActual, TInner>(IEqualityComparer<TInner?>? equalityComparer)
    : BaseAssertCondition<TActual> where TActual : IEnumerable<TInner>
{
    internal protected override string GetExpectation() => "items to be distinct";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        if (actualValue is null)
        {
            return AssertionResult.Fail($"{ActualExpression ?? typeof(IEnumerable<TInner>).Name} is null");
        }

        var list = actualValue.ToArray();

        var distinct = list.Distinct(equalityComparer);

        return AssertionResult
            .FailIf(list.Length != distinct.Count(),
                "duplicate items found in the collection");

    }
}