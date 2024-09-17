#nullable disable

using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static AssertionBuilder<bool, TAnd, TOr> IsTrue<TAnd, TOr>(this AssertionBuilder<bool, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<bool, TAnd, TOr>
        where TOr : IOr<bool, TAnd, TOr>
    {
        return new EqualsAssertCondition<bool, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), true)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<bool, TAnd, TOr> IsFalse<TAnd, TOr>(this AssertionBuilder<bool, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<bool, TAnd, TOr>
        where TOr : IOr<bool, TAnd, TOr>
    {
        return new EqualsAssertCondition<bool, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), false)
            .ChainedTo(assertionBuilder);
    }
}