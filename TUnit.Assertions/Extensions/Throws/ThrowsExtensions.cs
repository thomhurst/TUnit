using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class ThrowsExtensions
{
    public static ThrowsException<TActual> ThrowsException<TActual>(this IDelegateSource<TActual> delegateSource)
    {
        return new(delegateSource, exception => exception);
    }
    
    public static InvokableDelegateAssertionBuilder<TActual> ThrowsNothing<TActual>(this IDelegateSource<TActual> delegateSource)
    {
        return delegateSource.RegisterAssertion(new ThrowsNothingExpectedValueAssertCondition<TActual>()
            , []);
    }
}