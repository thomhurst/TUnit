#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Extensions;

public static class TimeOnlyIsExtensions
{
    public static TimeOnlyEqualToAssertionBuilderWrapper IsEqualTo(this IValueSource<TimeOnly> valueSource, TimeOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        return new TimeOnlyEqualToAssertionBuilderWrapper(
            valueSource.RegisterAssertion(new TimeOnlyEqualsExpectedValueAssertCondition(expected),
                [doNotPopulateThisValue1])
        );
    }
    
    public static InvokableValueAssertionBuilder<TimeOnly> IsAfter(this IValueSource<TimeOnly> valueSource,
        TimeOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeOnly, TimeOnly>(default,
                (value, _, _) => { return value > expected; },
                (value, _, _) =>
                    $"{Formatter.Format(value)} was not greater than {Formatter.Format(expected)}",
                $"to be after {expected}")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TimeOnly> IsAfterOrEqualTo(this IValueSource<TimeOnly> valueSource,
        TimeOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeOnly, TimeOnly>(default,
                (value, _, _) => { return value >= expected; },
                (value, _, _) =>
                    $"{Formatter.Format(value)} was not greater than or equal to {Formatter.Format(expected)}",
                $"to be after or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TimeOnly> IsBefore(this IValueSource<TimeOnly> valueSource,
        TimeOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeOnly, TimeOnly>(default,
                (value, _, _) => { return value < expected; },
                (value, _, _) =>
                    $"{Formatter.Format(value)} was not less than {Formatter.Format(expected)}",
                $"be before {expected}")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TimeOnly> IsBeforeOrEqualTo(this IValueSource<TimeOnly> valueSource,
        TimeOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeOnly, TimeOnly>(default,
                (value, _, _) => { return value <= expected; },
                (value, _, _) =>
                    $"{Formatter.Format(value)} was not less than or equal to {Formatter.Format(expected)}",
                $"be before or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
}