using System.Collections;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public class EnumerableCount<T, TInner> where T : IEnumerable<TInner> 
{
    protected AssertionBuilder<T> AssertionBuilder { get; }

    public EnumerableCount(AssertionBuilder<T> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public AssertCondition<T, int> EqualTo(int expected)
    {
        return new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, count, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) == count;
            },
            (enumerable, count, arg3) => $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {count}");
    }
    
    public AssertCondition<T, int> Empty =>
        new DelegateAssertCondition<T, int>(AssertionBuilder, 0, (enumerable, count, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) == count;
            },
            (enumerable, count, arg3) => $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {count}");

    public AssertCondition<T, int> GreaterThan(int expected)
    {
        return new DelegateAssertCondition<T, int>(
            AssertionBuilder, 
            expected, 
            (enumerable, count, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) > count;
            },
            (enumerable, count, arg3) => $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than {count}");
    }
    
    public AssertCondition<T, int> GreaterThanOrEqualTo(int expected)
    {
        return new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, count, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) >= count;
            },
            (enumerable, count, arg3) => $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than or equal to {count}");
    }
    
    public AssertCondition<T, int> LessThan(int expected)
    {
        return new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, count, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) < count;
            },
            (enumerable, count, arg3) => $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than {count}");
    }
    
    public AssertCondition<T, int> LessThanOrEqualTo(int expected)
    {
        return new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, count, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) <= count;
            },
            (enumerable, count, arg3) => $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than or equal to {count}");
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