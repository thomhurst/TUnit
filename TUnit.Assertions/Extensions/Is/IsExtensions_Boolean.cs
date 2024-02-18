#nullable disable

using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<bool, TAnd, TOr> True<TAnd, TOr>(this Is<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), true));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> False<TAnd, TOr>(this Is<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), false));
    }
}