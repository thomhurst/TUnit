#nullable disable

using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<bool, TAnd, TOr> True<TAnd, TOr>(this Is<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<bool, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), true));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> False<TAnd, TOr>(this Is<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<bool, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is, new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), false));
    }
}