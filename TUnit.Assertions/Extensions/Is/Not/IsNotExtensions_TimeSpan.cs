#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Is.Not;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> Zero<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new NotEqualsAssertCondition<TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), TimeSpan.Zero));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThan<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) => value <= expected,
            (value, _) => $"{value} was greater than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThan<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}"));
    }
}