#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Extensions;

public static class DateTimeOffsetIsExtensions
{
    public static DateTimeOffsetEqualToAssertionBuilderWrapper IsEqualTo(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        return new DateTimeOffsetEqualToAssertionBuilderWrapper(
            valueSource.RegisterAssertion(new DateTimeOffsetEqualsExpectedValueAssertCondition(expected),
                [doNotPopulateThisValue1])
        );
    }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsAfter(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{Formatter.Format(value)} was not greater than {Formatter.Format(expected)}",
            $"to be after {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsAfterOrEqualTo(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{Formatter.Format(value)} was not greater than or equal to {Formatter.Format(expected)}",
            $"to be after or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsBefore(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{Formatter.Format(value)} was not less than {Formatter.Format(expected)}",
            $"to be before {expected}")
            , [doNotPopulateThisValue]); }

    public static InvokableValueAssertionBuilder<DateTimeOffset> IsBeforeOrEqualTo(
        this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected,
        [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default,
                (value, _, _) => { return value <= expected; },
                (value, _, _) => $"{Formatter.Format(value)} was not less than or equal to {Formatter.Format(expected)}",
                $"to be before or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
}