#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Extensions;

public static class DateTimeOffsetIsExtensions
{
    public static DateTimeOffsetEqualToAssertion IsEqualTo(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        // Don't create condition yet! Just return configuration object
        return new DateTimeOffsetEqualToAssertion(valueSource, expected, [doNotPopulateThisValue1]);
    }

    public static AssertionBuilder<DateTimeOffset> IsAfter(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default(DateTimeOffset), (value, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{Formatter.Format(value)} was not greater than {Formatter.Format(expected)}",
            $"to be after {expected}")
            , [doNotPopulateThisValue]);
    }

    public static AssertionBuilder<DateTimeOffset> IsAfterOrEqualTo(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default(DateTimeOffset), (value, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{Formatter.Format(value)} was not greater than or equal to {Formatter.Format(expected)}",
            $"to be after or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public static AssertionBuilder<DateTimeOffset> IsBefore(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default(DateTimeOffset), (value, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{Formatter.Format(value)} was not less than {Formatter.Format(expected)}",
            $"to be before {expected}")
            , [doNotPopulateThisValue]);
    }

    public static AssertionBuilder<DateTimeOffset> IsBeforeOrEqualTo(
        this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected,
        [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default(DateTimeOffset),
                (value, _, _) => { return value <= expected; },
                (value, _, _) => $"{Formatter.Format(value)} was not less than or equal to {Formatter.Format(expected)}",
                $"to be before or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
}
