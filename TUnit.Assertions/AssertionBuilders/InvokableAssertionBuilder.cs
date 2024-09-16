using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class InvokableAssertionBuilder<TActual, TAnd, TOr>  : AssertionBuilder<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected InvokableAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate) : base(assertionDataDelegate)
    {
    }

    protected InvokableAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, string actual) : base(assertionDataDelegate, actual)
    {
    }

    public TaskAwaiter GetAwaiter() => ProcessAssertionsAsync().GetAwaiter();

    private protected abstract Task ProcessAssertionsAsync();
}