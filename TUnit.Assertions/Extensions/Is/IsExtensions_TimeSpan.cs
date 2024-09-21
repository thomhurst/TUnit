#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableValueAssertionBuilder<TimeSpan> IsBetween(this IValueSource<TimeSpan> valueSource, TimeSpan lowerBound, TimeSpan upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _, _) => $"{value} was not between {lowerBound} and {upperBound}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsEqualToWithTolerance(this IValueSource<TimeSpan> valueSource, TimeSpan expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeSpan, TimeSpan>(expected,
            (actual, _, _, _) =>
            {
                return actual <= expected.Add(tolerance) && actual >= expected.Subtract(tolerance);
            },
            (timeSpan, _, _) => $"{timeSpan} is not between {timeSpan.Subtract(tolerance)} and {timeSpan.Add(tolerance)}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsZero(this IValueSource<TimeSpan> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsAssertCondition<TimeSpan>(TimeSpan.Zero)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsGreaterThan(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value} was not greater than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsGreaterThanOrEqualTo(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value} was not greater than or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsLessThan(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value} was not less than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsLessThanOrEqualTo(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value} was not less than or equal to {expected}")
            , [doNotPopulateThisValue]); }
}