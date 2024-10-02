using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableValueAssertionBuilder<TActual> : InvokableAssertionBuilder<TActual>, IValueSource<TActual>
{
    internal InvokableValueAssertionBuilder(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder.AssertionDataDelegate, invokableAssertionBuilder)
    {
    }

    public AssertionBuilder<TActual> AssertionBuilder => this;
    
    public ValueAnd<TActual> And => new(AssertionBuilder.AppendConnector(ChainType.And));
    public ValueOr<TActual> Or => new(AssertionBuilder.AppendConnector(ChainType.Or));
}