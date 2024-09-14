#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> IsBetween<TAnd, TOr>(this IIs<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset lowerBound, DateTimeOffset upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<DateTimeOffset, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not between {lowerBound.ToLongStringWithMilliseconds()} and {upperBound.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> IsGreaterThan<TAnd, TOr>(this IIs<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<DateTimeOffset, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> IsGreaterThanOrEqualTo<TAnd, TOr>(this IIs<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<DateTimeOffset, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> IsLessThan<TAnd, TOr>(this IIs<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<DateTimeOffset, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> IsLessThanOrEqualTo<TAnd, TOr>(this IIs<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<DateTimeOffset, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this IIs<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<DateTimeOffset, TAnd, TOr>
    {
        return IsBetween(@is, expected - tolerance, expected + tolerance, doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}