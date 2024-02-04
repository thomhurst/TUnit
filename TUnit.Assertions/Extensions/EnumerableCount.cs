using System.Collections;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public class EnumerableCount<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr> 
    where TActual : IEnumerable
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public EnumerableCount(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder, expected, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) == expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be equal to {expected}")
        );
    }

    public BaseAssertCondition<TActual, TAnd, TOr> Empty =>
        Wrap(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder, 0, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) == expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be equal to {0}")
        );

    public BaseAssertCondition<TActual, TAnd, TOr> GreaterThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<TActual, int, TAnd, TOr>(
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

    public BaseAssertCondition<TActual, TAnd, TOr> GreaterThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder, expected, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) >= expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be greater than or equal to {expected}")
        );
    }

    public BaseAssertCondition<TActual, TAnd, TOr> LessThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder, expected, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) < expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be less than {expected}")
        );
    }

    public BaseAssertCondition<TActual, TAnd, TOr> LessThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder, expected, (enumerable, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(enumerable);
                return GetCount(enumerable) <= expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a expected of {GetCount(enumerable)} but expected to be less than or equal to {expected}")
        );
    }

    private int GetCount(TActual? actualValue)
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