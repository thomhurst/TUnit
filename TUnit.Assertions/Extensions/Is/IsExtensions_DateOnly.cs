#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<DateOnly, TAnd, TOr> IsGreaterThan<TAnd, TOr>(this IIs<DateOnly, TAnd, TOr> @is, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<DateOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> IsGreaterThanOrEqualTo<TAnd, TOr>(this IIs<DateOnly, TAnd, TOr> @is, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<DateOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> IsLessThan<TAnd, TOr>(this IIs<DateOnly, TAnd, TOr> @is, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<DateOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> IsLessThanOrEqualTo<TAnd, TOr>(this IIs<DateOnly, TAnd, TOr> @is, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<DateOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> IsBetween<TAnd, TOr>(this IIs<DateOnly, TAnd, TOr> @is, DateOnly lowerBound, DateOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<DateOnly, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this IIs<DateOnly, TAnd, TOr> @is, DateOnly expected, int daysTolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("daysTolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<DateOnly, TAnd, TOr>
    {
        return IsBetween(@is, expected.AddDays(-daysTolerance), expected.AddDays(daysTolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}