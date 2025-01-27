using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountEqualToExpectedValueAssertCondition<TInner>(int expected)
    : ExpectedValueAssertCondition<IEnumerable<TInner>, int>(expected)
{
    protected override string GetExpectation() => $"to have a count of {expected}";

    protected override AssertionResult GetResult(IEnumerable<TInner>? actualValue, int count)
    {
        var actualCount = GetCount(actualValue);

        return AssertionResult
            .FailIf(actualValue is null,
                $"{ActualExpression ?? typeof(IEnumerable<TInner>).Name} is null")
            .OrFailIf(actualCount != count,
                $"it was {actualCount} instead");
    }

    private int GetCount(IEnumerable<TInner>? actualValue)
    {
        if (actualValue is ICollection collection)
        {
            return collection.Count;
        }
        
        return actualValue?.Cast<object>().Count() ?? 0;
    }
}