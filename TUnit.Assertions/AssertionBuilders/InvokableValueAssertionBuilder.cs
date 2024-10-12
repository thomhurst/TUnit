using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableValueAssertionBuilder<TActual>(InvokableAssertionBuilder<TActual> invokableAssertionBuilder)
    : InvokableAssertionBuilder<TActual>(invokableAssertionBuilder)
{
    /// <summary>
    /// Provide a reason explaining why the assertion is needed.<br />
    /// If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </summary>
    public InvokableValueAssertionBuilder<TActual> Because(string reason)
    {
        var becauseReason = new BecauseReason(reason);
        var assertion = Assertions.Peek();
        assertion.SetBecauseReason(becauseReason);
        return this;
    }

    internal AssertionBuilder<TActual> AssertionBuilder => this;
    
    public ValueAnd<TActual> And => new(new AndAssertionBuilder<TActual>(AssertionBuilder.AppendConnector(ChainType.And)));
    public ValueOr<TActual> Or => new(new OrAssertionBuilder<TActual>(AssertionBuilder.AppendConnector(ChainType.Or)));
    
    public new TaskAwaiter<TActual?> GetAwaiter()
    {
        return AssertAndGet().GetAwaiter();
    }

    private async Task<TActual?> AssertAndGet()
    {
        var data = await ProcessAssertionsAsync();
        return data.Result;
    }
}