namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableDistinctItemsExpectedValueAssertCondition<TInner>(IEqualityComparer<TInner?>? equalityComparer)
    : BaseAssertCondition<IEnumerable<TInner>>
{
    protected override string GetExpectation() => "items to be distinct";

    protected override ValueTask<AssertionResult> GetResult(
        IEnumerable<TInner>? actualValue, Exception? exception,
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