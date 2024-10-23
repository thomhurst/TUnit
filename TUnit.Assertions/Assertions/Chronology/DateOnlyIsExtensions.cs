#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static class DateOnlyIsExtensions
{
    public static DateOnlyEqualToAssertionBuilderWrapper IsEqualTo(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        return new DateOnlyEqualToAssertionBuilderWrapper(
            valueSource.RegisterAssertion(new DateOnlyEqualsExpectedValueAssertCondition(expected),
                [doNotPopulateThisValue1])
        );
    }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsAfter(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default, (value, _, _) => value > expected,
            (value, _, _) => $"{value} was not greater than {expected}",
            $"to be after {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsAfterOrEqualTo(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default, (value, _, _) => value >= expected,
            (value, _, _) => $"{value} was not greater than or equal to {expected}",
            $"to be after or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsBefore(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default, (value, _, _) => value < expected,
            (value, _, _) => $"{value} was not less than {expected}",
            $"to be before {expected}")
            , [doNotPopulateThisValue]); }

    public static InvokableValueAssertionBuilder<DateOnly> IsBeforeOrEqualTo(this IValueSource<DateOnly> valueSource,
        DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default,
                (value, _, _) => value <= expected,
                (value, _, _) => $"{value} was not less than or equal to {expected}",
                $"to be before or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
}