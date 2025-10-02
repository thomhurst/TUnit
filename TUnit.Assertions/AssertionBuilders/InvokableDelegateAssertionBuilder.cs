using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Invokable delegate assertion - supports awaiting and provides delegate-specific And/Or chaining
/// </summary>
public class InvokableDelegateAssertion : InvokableAssertion<object?>, IDelegateSource
{
    internal InvokableDelegateAssertion(InvokableAssertion<object?> invokableAssertion) : base(invokableAssertion)
    {
    }

    public AssertionCore AssertionCore => this;

    public DelegateAnd<object?> And => new(new AndAssertion(AssertionCore.AppendConnector(ChainType.And)));
    public DelegateOr<object?> Or => new(new OrAssertion(AssertionCore.AppendConnector(ChainType.Or)));
}
