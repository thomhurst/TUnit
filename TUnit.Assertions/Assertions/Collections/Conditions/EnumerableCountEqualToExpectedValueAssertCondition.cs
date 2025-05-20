using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountEqualToExpectedValueAssertCondition<TActual, TInner>(int expected)
    : ExpectedValueAssertCondition<TActual, int>(expected) where TActual : IEnumerable<TInner>
{
    internal protected override string GetExpectation() => $"to have a count of {ExpectedValue}";

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, int count)
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