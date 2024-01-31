using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class HasCountAssertCondition<TActual> : AssertCondition<IEnumerable<TActual>, int>
{
    public HasCountAssertCondition(int expected) : base(expected)
    {
    }

    protected override string DefaultMessage => $"Length is {GetCount(ActualValue)} instead of {ExpectedValue}";
    
    protected internal override bool Passes(IEnumerable<TActual>? actualValue)
    {
        return GetCount(actualValue) == ExpectedValue;
    }

    private int GetCount(IEnumerable<TActual>? actualValue)
    {
        ArgumentNullException.ThrowIfNull(actualValue);

        if (actualValue is ICollection collection)
        {
            return collection.Count;
        }

        if (actualValue is TActual[] array)
        {
            return array.Length;
        }
        
        return actualValue.Count();
    }
}