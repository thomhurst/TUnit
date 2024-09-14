#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsNotZero<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new NotEqualsAssertCondition<TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), TimeSpan.Zero));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsNotGreaterThan<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) => value <= expected,
            (value, _) => $"{value} was greater than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsNotGreaterThanOrEqualTo<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsNotLessThan<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> IsNotLessThanOrEqualTo<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TimeSpan, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}"));
    }
}