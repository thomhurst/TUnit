using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Throws;

public static class ThrowsExtensions
{
    public static ThrowsException<TActual, TAnd, TOr> ThrowsException<TActual, TAnd, TOr>(this IDelegateAssertionBuilder<TActual, TAnd, TOr> delegateAssertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new(assertionBuilder, delegateAssertionBuilder.AssertionConnector.ChainType, delegateAssertionBuilder.AssertionConnector.OtherAssertCondition, exception => exception);
    }
    
    public static TOutput ThrowsNothing<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this IDelegateAssertionBuilder<TActual, TAnd, TOr> delegateAssertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new ThrowsNothingAssertCondition<TActual, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(string.Empty));
    }
}