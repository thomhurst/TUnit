using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableDelegateAssertionBuilder : InvokableAssertionBuilder<object?>
{
    internal InvokableDelegateAssertionBuilder(InvokableAssertionBuilder<object?> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public AssertionBuilder AssertionBuilder => this;
    
    public DelegateAnd<object?> And => new(new AndAssertionBuilder(AssertionBuilder.AppendConnector(ChainType.And)));
    public DelegateOr<object?> Or => new(new OrAssertionBuilder(AssertionBuilder.AppendConnector(ChainType.Or)));
}