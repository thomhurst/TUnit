#nullable disable

using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static TOutput IsTrue<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TAnd : IAnd<bool, TAnd, TOr>
        where TOr : IOr<bool, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<bool, TAnd, TOr>, IOutputsChain<TOutput, bool>
        where TOutput : InvokableAssertionBuilder<bool, TAnd, TOr>
    {
        return new EqualsAssertCondition<bool, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), true)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsFalse<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TAnd : IAnd<bool, TAnd, TOr>
        where TOr : IOr<bool, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<bool, TAnd, TOr>, IOutputsChain<TOutput, bool>
        where TOutput : InvokableAssertionBuilder<bool, TAnd, TOr>
    {
        return new EqualsAssertCondition<bool, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), false)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
}