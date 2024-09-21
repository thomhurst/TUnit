using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableDelegateAssertionBuilder<TActual> : InvokableAssertionBuilder<TActual>, IDelegateSource<TActual>
{
    internal InvokableDelegateAssertionBuilder(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder.AssertionDataDelegate, invokableAssertionBuilder)
    {
    }

    public AssertionBuilder<TActual> AssertionBuilder => this;
    
    public DelegateAnd<TActual> And => new(AssertionBuilder.AppendConnector(ChainType.And));
    public DelegateOr<TActual> Or => new(AssertionBuilder.AppendConnector(ChainType.Or));
}