#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class DateOnlyIsExtensions
{
    public static InvokableValueAssertionBuilder<DateOnly> IsAfter(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default, (value, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value} was not greater than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsAfterOrEqualTo(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default, (value, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value} was not greater than or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsBefore(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default, (value, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value} was not less than {expected}")
            , [doNotPopulateThisValue]); }

    public static InvokableValueAssertionBuilder<DateOnly> IsBeforeOrEqualTo(this IValueSource<DateOnly> valueSource,
        DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateOnly, DateOnly>(default,
                (value, _, _) => { return value <= expected; },
                (value, _, _) => $"{value} was not less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
}