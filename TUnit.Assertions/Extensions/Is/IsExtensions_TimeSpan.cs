#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsBetween<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan lowerBound, TimeSpan upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}")
            .ChainedTo(assertionBuilder); }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan,TimeSpan,TAnd,TOr>(
            assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                return actual <= expected.Add(tolerance) && actual >= expected.Subtract(tolerance);
            },
            (timeSpan, _) => $"{timeSpan} is not between {timeSpan.Subtract(tolerance)} and {timeSpan.Add(tolerance)}")
            .ChainedTo(assertionBuilder); }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsZero<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new EqualsAssertCondition<TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), TimeSpan.Zero)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsGreaterThan<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}")
            .ChainedTo(assertionBuilder); }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsGreaterThanOrEqualTo<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}")
            .ChainedTo(assertionBuilder); }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsLessThan<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}")
            .ChainedTo(assertionBuilder); }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsLessThanOrEqualTo<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}")
            .ChainedTo(assertionBuilder); }
}