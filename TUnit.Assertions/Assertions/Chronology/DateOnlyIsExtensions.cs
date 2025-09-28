#nullable disable

#if NET

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class DateOnlyIsExtensions
{
    public static DateOnlyEqualToAssertion IsEqualTo(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        // Don't create condition yet! Just return configuration object
        return new DateOnlyEqualToAssertion(valueSource, expected, [doNotPopulateThisValue1]);
    }
    
    public static AssertionBuilder<DateOnly> IsAfter(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default(DateOnly), (value, _, _) => value > expected,
            (value, _, _) => $"{value} was not greater than {expected}",
            $"to be after {expected}")
            , [doNotPopulateThisValue]); }
    
    public static AssertionBuilder<DateOnly> IsAfterOrEqualTo(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default(DateOnly), (value, _, _) => value >= expected,
            (value, _, _) => $"{value} was not greater than or equal to {expected}",
            $"to be after or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static AssertionBuilder<DateOnly> IsBefore(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default(DateOnly), (value, _, _) => value < expected,
            (value, _, _) => $"{value} was not less than {expected}",
            $"to be before {expected}")
            , [doNotPopulateThisValue]); }

    public static AssertionBuilder<DateOnly> IsBeforeOrEqualTo(this IValueSource<DateOnly> valueSource,
        DateOnly expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default(DateOnly),
                (value, _, _) => value <= expected,
                (value, _, _) => $"{value} was not less than or equal to {expected}",
                $"to be before or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
}

#endif
