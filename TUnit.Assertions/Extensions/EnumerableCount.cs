using System.Collections;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public class EnumerableCount<T> : Connector<T> where T : IEnumerable
{
    protected internal AssertionBuilder<T> AssertionBuilder { get; }

    public EnumerableCount(AssertionBuilder<T> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<T>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<T> EqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, count, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) == count;
            },
            (enumerable, count, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {count}")
        );
    }

    public BaseAssertCondition<T> Empty =>
        Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, 0, (enumerable, count, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) == count;
            },
            (enumerable, count, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {count}")
        );

    public BaseAssertCondition<T> GreaterThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(
            AssertionBuilder,
            expected,
            (enumerable, count, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) > count;
            },
            (enumerable, count, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than {count}")
        );
    }

    public BaseAssertCondition<T> GreaterThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, count, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) >= count;
            },
            (enumerable, count, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than or equal to {count}")
        );
    }

    public BaseAssertCondition<T> LessThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, count, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) < count;
            },
            (enumerable, count, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than {count}")
        );
    }

    public BaseAssertCondition<T> LessThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, count, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) <= count;
            },
            (enumerable, count, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than or equal to {count}")
        );
    }

    private int GetCount(T? actualValue)
    {
        ArgumentNullException.ThrowIfNull(actualValue);

        if (actualValue is ICollection collection)
        {
            return collection.Count;
        }

        if (actualValue is IList list)
        {
            return list.Count;
        }

        return actualValue.Cast<object>().Count();
    }
}