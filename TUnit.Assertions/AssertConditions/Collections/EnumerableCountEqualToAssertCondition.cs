using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountEqualToAssertCondition<T, TInner> : AssertCondition<T, int>
    where T : IEnumerable<TInner>
{
    public EnumerableCountEqualToAssertCondition(AssertionBuilder<T> assertionBuilder, int expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => $"Length is {GetCount(ActualValue)} instead of {ExpectedValue}";
    
    protected internal override bool Passes(T? actualValue, Exception? exception)
    {
        return GetCount(actualValue) == ExpectedValue;
    }

    private int GetCount(T? actualValue)
    {
        ArgumentNullException.ThrowIfNull(actualValue);

        if (actualValue is ICollection collection)
        {
            return collection.Count;
        }

        if (actualValue is TInner[] array)
        {
            return array.Length;
        }
        
        return actualValue.Count();
    }
}