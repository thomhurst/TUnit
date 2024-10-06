using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountEqualToExpectedValueAssertCondition<TActual>(int expected)
    : ExpectedValueAssertCondition<TActual, int>(expected)
    where TActual : IEnumerable
{
    protected override string GetFailureMessage(TActual? actualValue, int count) => $"Length is {GetCount(actualValue)} instead of {count}";
    
    protected override bool Passes(TActual? actualValue, int count)
    {
        if (actualValue is null)
        {
            return FailWithMessage($"{ActualExpression ?? typeof(TActual).Name} is null");
        }
        
        return GetCount(actualValue) == count;
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