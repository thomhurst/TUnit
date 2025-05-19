using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableValueAssertionBuilder<TActual>(ISource source) : InvokableAssertionBuilder<TActual>(source), IValueSource<TActual>
{
    /// <summary>
    /// Provide a reason explaining why the assertion is needed.<br />
    /// If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </summary>
    public InvokableValueAssertionBuilder<TActual> Because(string reason)
    {
        var becauseReason = new BecauseReason(reason);
        var assertion = Source.Assertions.Peek();
        assertion.SetBecauseReason(becauseReason);
        return this;
    }

    internal AssertionBuilder AssertionBuilder => this;
    
    public ValueAnd<TActual> And => new(new AndAssertionBuilder(AssertionBuilder.AppendConnector(ChainType.And)));
    public ValueOr<TActual> Or => new(new OrAssertionBuilder(AssertionBuilder.AppendConnector(ChainType.Or)));
    
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