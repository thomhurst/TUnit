#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<DateTime, TAnd, TOr> Between<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime lowerBound, DateTime upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<DateTime, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not between {lowerBound.ToLongStringWithMilliseconds()} and {upperBound.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<DateTime, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<DateTime, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> LessThan<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<DateTime, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<DateTime, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<DateTime, TAnd, TOr>
    {
        return Between(@is, expected - tolerance, expected + tolerance, doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}