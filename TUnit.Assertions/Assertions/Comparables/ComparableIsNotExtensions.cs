#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Comparable;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class ComparableIsNotExtensions
{
    public static AssertionBuilder<TActual> IsNotGreaterThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, _) =>
            {
                return value.CompareTo(expected) <= 0;
            },
            (value, _, _) => $"{value} was greater than {expected}",
            $"to not be greater than {expected}")
            , [doNotPopulateThisValue]);
    }

    public static AssertionBuilder<TActual> IsNotGreaterThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, _) =>
            {
                return value.CompareTo(expected) < 0;
            },
            (value, _, _) => $"{value} was greater than or equal to {expected}",
            $"to not be greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public static AssertionBuilder<TActual> IsNotLessThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, _) =>
            {
                return value.CompareTo(expected) >= 0;
            },
            (value, _, _) => $"{value} was less than {expected}",
            $"to not be less than {expected}")
            , [doNotPopulateThisValue]);
    }

    public static AssertionBuilder<TActual> IsNotLessThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, _) =>
            {
                return value.CompareTo(expected) > 0;
            },
            (value, _, _) => $"{value} was less than or equal to {expected}",
            $"to not be less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public static NotBetweenAssertion<TActual> IsNotBetween<TActual>(this IValueSource<TActual> valueSource, TActual lowerBound, TActual upperBound, [CallerArgumentExpression(nameof(lowerBound))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(upperBound))] string doNotPopulateThisValue2 = null)
        where TActual : IComparable<TActual>
    {
        var assertionBuilder = valueSource.RegisterAssertion(new NotBetweenAssertCondition<TActual>(lowerBound, upperBound)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);

        return new NotBetweenAssertion<TActual>(assertionBuilder);
    }
}
