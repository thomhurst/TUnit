#nullable disable

using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<bool, TAnd, TOr> IsNotTrue<TAnd, TOr>(this IsNot<bool, TAnd, TOr> isNot)
        where TAnd : And<bool, TAnd, TOr>, IAnd<bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<bool, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new EqualsAssertCondition<bool, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), false));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> IsNotFalse<TAnd, TOr>(this IsNot<bool, TAnd, TOr> isNot)
        where TAnd : And<bool, TAnd, TOr>, IAnd<bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<bool, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot, new EqualsAssertCondition<bool, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), true));
    }
}