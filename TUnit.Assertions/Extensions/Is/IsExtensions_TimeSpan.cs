#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> Between<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan lowerBound, TimeSpan upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<TimeSpan,TimeSpan,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                return actual <= expected.Add(tolerance) && actual >= expected.Subtract(tolerance);
            },
            (timeSpan, _) => $"{timeSpan} is not between {timeSpan.Subtract(tolerance)} and {timeSpan.Add(tolerance)}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> Zero<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new EqualsAssertCondition<TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), TimeSpan.Zero));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThan<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
}