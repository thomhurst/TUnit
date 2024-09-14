#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> IsGreaterThan<TAnd, TOr>(this IIs<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TimeOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> IsGreaterThanOrEqualTo<TAnd, TOr>(this IIs<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TimeOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> IsLessThan<TAnd, TOr>(this IIs<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TimeOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> IsLessThanOrEqualTo<TAnd, TOr>(this IIs<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TimeOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> IsBetween<TAnd, TOr>(this IIs<TimeOnly, TAnd, TOr> @is, TimeOnly lowerBound, TimeOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TimeOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) => value >= lowerBound && value <= upperBound,
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this IIs<TimeOnly, TAnd, TOr> @is, TimeOnly expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TimeOnly, TAnd, TOr>
    {
        return IsBetween(@is, expected.Add(-tolerance), expected.Add(tolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}