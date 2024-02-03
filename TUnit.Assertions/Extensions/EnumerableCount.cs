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
        return Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) == expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be equal to {expected}")
        );
    }

    public BaseAssertCondition<T> Empty =>
        Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, 0, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) == expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be equal to {0}")
        );

    public BaseAssertCondition<T> GreaterThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(
            AssertionBuilder,
            expected,
            (enumerable, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) > expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be greater than {expected}")
        );
    }

    public BaseAssertCondition<T> GreaterThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) >= expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be greater than or equal to {expected}")
        );
    }

    public BaseAssertCondition<T> LessThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) < expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be less than {expected}")
        );
    }

    public BaseAssertCondition<T> LessThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<T, int>(AssertionBuilder, expected, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) <= expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be less than or equal to {expected}")
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