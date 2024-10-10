using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountEqualToExpectedValueAssertCondition<TActual>(int expected)
    : ExpectedValueAssertCondition<TActual, int>(expected)
    where TActual : IEnumerable
{
    protected override string GetExpectation() => $"to have a count of {expected}";

    protected override AssertionResult GetResult(TActual? actualValue, int count)
    {
        var actualCount = GetCount(actualValue);

        return AssertionResult
            .FailIf(
                () => actualValue is null,
                $"{ActualExpression ?? typeof(TActual).Name} is null")
            .OrFailIf(
                () => actualCount != count,
                $"it was {actualCount} instead");
    }

    private int GetCount(TActual? actualValue)
    {
        if (actualValue is ICollection collection)
        {
            return collection.Count;
        }
        
        return actualValue?.Cast<object>().Count() ?? 0;
    }
}