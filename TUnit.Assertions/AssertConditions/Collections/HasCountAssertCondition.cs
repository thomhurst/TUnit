using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class HasCountAssertCondition<TActual> : AssertCondition<IEnumerable<TActual>, int>
{
    public HasCountAssertCondition(IReadOnlyCollection<AssertCondition<IEnumerable<TActual>, int>> nestedConditions, NestedConditionsOperator? @operator, int expected) : base(expected)
    {
    }

    public override string DefaultMessage => $"Length is {GetCount(ActualValue)} instead of {ExpectedValue}";
    
    protected internal override bool Passes(IEnumerable<TActual> actualValue)
    {
        return GetCount(actualValue) == ExpectedValue;
    }

    private int GetCount(IEnumerable<TActual> actualValue)
    {
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