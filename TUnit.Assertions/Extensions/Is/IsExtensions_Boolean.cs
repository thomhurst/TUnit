#nullable disable

using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<bool, TAnd, TOr> IsTrue<TAnd, TOr>(this IIs<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<bool, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), true));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> IsFalse<TAnd, TOr>(this IIs<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<bool, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), false));
    }
}