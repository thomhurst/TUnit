#nullable disable

using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<bool, TAnd, TOr> IsTrue<TAnd, TOr>(this IValueSource<bool, TAnd, TOr> valueSource)
        where TAnd : IAnd<bool, TAnd, TOr>
        where TOr : IOr<bool, TAnd, TOr>
    {
        return new EqualsAssertCondition<bool, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(null), true)
            .ChainedTo(valueSource.AssertionBuilder);
    }
    
    public static InvokableAssertionBuilder<bool, TAnd, TOr> IsFalse<TAnd, TOr>(this IValueSource<bool, TAnd, TOr> valueSource)
        where TAnd : IAnd<bool, TAnd, TOr>
        where TOr : IOr<bool, TAnd, TOr>
    {
        return new EqualsAssertCondition<bool, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(null), false)
            .ChainedTo(valueSource.AssertionBuilder);
    }
}