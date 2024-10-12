using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableDelegateAssertionBuilder<TActual> : InvokableAssertionBuilder<TActual>
{
    internal InvokableDelegateAssertionBuilder(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public AssertionBuilder<TActual> AssertionBuilder => this;
    
    public DelegateAnd<TActual> And => new(new AndAssertionBuilder<TActual>(AssertionBuilder.AppendConnector(ChainType.And)));
    public DelegateOr<TActual> Or => new(new OrAssertionBuilder<TActual>(AssertionBuilder.AppendConnector(ChainType.Or)));
}