#nullable disable

using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Is.Not;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<bool, TAnd, TOr> True<TAnd, TOr>(this IsNot<bool, TAnd, TOr> isNot)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return isNot.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), false));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> False<TAnd, TOr>(this IsNot<bool, TAnd, TOr> isNot)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return isNot.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), true));
    }
}