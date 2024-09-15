#nullable disable

using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<bool, TAnd, TOr> IsNotTrue<TAnd, TOr>(this IIs<bool, TAnd, TOr> isNot)
        where TAnd : And<bool, TAnd, TOr>, IAnd<bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<bool, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.IsNot(), new EqualsAssertCondition<bool, TAnd, TOr>(isNot.IsNot().AssertionBuilder.AppendCallerMethod(null), false));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> IsNotFalse<TAnd, TOr>(this IIs<bool, TAnd, TOr> isNot)
        where TAnd : And<bool, TAnd, TOr>, IAnd<bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<bool, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.IsNot(), new EqualsAssertCondition<bool, TAnd, TOr>(isNot.IsNot().AssertionBuilder.AppendCallerMethod(null), true));
    }
}