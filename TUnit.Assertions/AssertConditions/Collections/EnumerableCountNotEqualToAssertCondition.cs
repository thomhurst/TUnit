using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountNotEqualToAssertCondition<TActual> : AssertCondition<TActual, int>
    where TActual : IEnumerable
{
    public EnumerableCountNotEqualToAssertCondition(int expected) : base(expected)
    {
    }

    protected internal override string GetFailureMessage() => $"Count is {ExpectedValue}";
    
    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            OverriddenMessage = $"{RawActualExpression ?? typeof(TActual).Name} is null";
            return false;
        }
        
        return GetCount(actualValue) != ExpectedValue;
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