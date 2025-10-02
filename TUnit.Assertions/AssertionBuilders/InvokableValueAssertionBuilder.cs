using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Invokable value assertion - supports awaiting and provides value-specific And/Or chaining
/// </summary>
public class InvokableValueAssertion<TActual>(ISource source) : InvokableAssertion<TActual>(source), IValueSource<TActual>
{
    /// <summary>
    /// Provide a reason explaining why the assertion is needed.<br />
    /// If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </summary>
    public InvokableValueAssertion<TActual> Because(string reason)
    {
        var becauseReason = new BecauseReason(reason);
        var assertion = Source.Assertions.Peek();
        assertion.SetBecauseReason(becauseReason);
        return this;
    }

    internal AssertionCore AssertionCore => this;

    public ValueAnd<TActual> And => new(new AndAssertion(AssertionCore.AppendConnector(ChainType.And)));
    public ValueOr<TActual> Or => new(new OrAssertion(AssertionCore.AppendConnector(ChainType.Or)));

    public new TaskAwaiter<TActual?> GetAwaiter()
    {
        return AssertAndGet().GetAwaiter();
    }

    private async Task<TActual?> AssertAndGet()
    {
        var data = await ProcessAssertionsAsync();
        return (TActual?) data.Result;
    }
}
