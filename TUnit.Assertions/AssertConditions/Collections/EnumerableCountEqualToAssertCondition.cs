using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountEqualToAssertCondition<TActual> : AssertCondition<TActual, int>
    where TActual : IEnumerable
{
    public EnumerableCountEqualToAssertCondition(int expected) : base(expected)
    {
    }

    protected internal override string GetFailureMessage() => $"Length is {GetCount(ActualValue)} instead of {ExpectedValue}";
    
    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            OverriddenMessage = $"{RawActualExpression ?? typeof(TActual).Name} is null";
            return false;
        }
        
        return GetCount(actualValue) == ExpectedValue;
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