#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsBetween<TAnd, TOr>(this IIs<TimeSpan, TAnd, TOr> @is, TimeSpan lowerBound, TimeSpan upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this IIs<TimeSpan, TAnd, TOr> @is, TimeSpan expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeSpan,TimeSpan,TAnd,TOr>(
            @is.Is().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                return actual <= expected.Add(tolerance) && actual >= expected.Subtract(tolerance);
            },
            (timeSpan, _) => $"{timeSpan} is not between {timeSpan.Subtract(tolerance)} and {timeSpan.Add(tolerance)}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsZero<TAnd, TOr>(this IIs<TimeSpan, TAnd, TOr> @is)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new EqualsAssertCondition<TimeSpan, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(null), TimeSpan.Zero));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsGreaterThan<TAnd, TOr>(this IIs<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsGreaterThanOrEqualTo<TAnd, TOr>(this IIs<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsLessThan<TAnd, TOr>(this IIs<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsLessThanOrEqualTo<TAnd, TOr>(this IIs<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
}