using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountNotEqualToAssertCondition<TActual> : AssertCondition<TActual, int>
    where TActual : IEnumerable
{
    public EnumerableCountNotEqualToAssertCondition(int expected) : base(expected)
    {
    }

    protected override string DefaultMessage => $"Count is {ExpectedValue}";
    
    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        if (actualValue is null)
        {
            WithMessage((_, _, actualExpression) => $"{actualExpression ?? typeof(TActual).Name} is null");
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