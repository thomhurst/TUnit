#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsBetween<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan lowerBound, TimeSpan upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _, _) => $"{value} was not between {lowerBound} and {upperBound}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan,TimeSpan,TAnd,TOr>(expected,
            (actual, _, _, _) =>
            {
                return actual <= expected.Add(tolerance) && actual >= expected.Subtract(tolerance);
            },
            (timeSpan, _, _) => $"{timeSpan} is not between {timeSpan.Subtract(tolerance)} and {timeSpan.Add(tolerance)}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsZero<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource)
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new EqualsAssertCondition<TimeSpan, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(null), TimeSpan.Zero)
            .ChainedTo(valueSource.AssertionBuilder);
    }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsGreaterThan<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value} was not greater than {expected}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsGreaterThanOrEqualTo<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value} was not greater than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsLessThan<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value} was not less than {expected}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsLessThanOrEqualTo<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value} was not less than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder); }
}