#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableValueAssertionBuilder<DateOnly> IsAfter(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateOnly, DateOnly>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value} was not greater than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsAfterOrEqualTo(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateOnly, DateOnly>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value} was not greater than or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsBefore(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateOnly, DateOnly>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value} was not less than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsBeforeOrEqualTo(this IValueSource<DateOnly> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateOnly, DateOnly>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value} was not less than or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsBetween(this IValueSource<DateOnly> valueSource, DateOnly lowerBound, DateOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateOnly, DateOnly>(default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _, _) => $"{value} was not between {lowerBound} and {upperBound}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableValueAssertionBuilder<DateOnly> IsEqualToWithTolerance(this IValueSource<DateOnly> valueSource, DateOnly expected, int daysTolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("daysTolerance")] string doNotPopulateThisValue2 = "")
    {
        return IsBetween(valueSource, expected.AddDays(-daysTolerance), expected.AddDays(daysTolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}