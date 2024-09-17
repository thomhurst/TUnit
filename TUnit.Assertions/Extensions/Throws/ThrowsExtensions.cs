using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Throws;

public static class ThrowsExtensions
{
    public static ThrowsException<TActual, TAnd, TOr> ThrowsException<TActual, TAnd, TOr>(this IDelegateSource<TActual, TAnd, TOr> delegateSource)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new(delegateSource.AssertionBuilder, exception => exception);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> ThrowsNothing<TActual, TAnd, TOr>(this IDelegateSource<TActual, TAnd, TOr> delegateSource)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new ThrowsNothingAssertCondition<TActual, TAnd, TOr>()
            .ChainedTo(delegateSource.AssertionBuilder, []);
    }
}