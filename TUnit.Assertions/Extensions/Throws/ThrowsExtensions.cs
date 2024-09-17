using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Throws;

public static class ThrowsExtensions
{
    public static ThrowsException<TActual, TAnd, TOr> ThrowsException<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> delegateAssertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new(delegateAssertionBuilder, exception => exception);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> ThrowsNothing<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> delegateAssertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new ThrowsNothingAssertCondition<TActual, TAnd, TOr>(
                delegateAssertionBuilder.AppendCallerMethod(string.Empty))
            .ChainedTo(delegateAssertionBuilder);
    }
}