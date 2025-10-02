using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableValueDelegateAssertionBuilder<TActual> : InvokableAssertion<TActual>
{
    internal InvokableValueDelegateAssertionBuilder(ValueTask<AssertionData> assertionDataDelegate, AssertionCore assertionCore) : base(assertionCore)
    {
    }

    public AssertionCore AssertionCore => this;

    public ValueDelegateAnd<TActual> And => new(AssertionCore.AppendConnector(ChainType.And));
    public ValueDelegateOr<TActual> Or => new(AssertionCore.AppendConnector(ChainType.Or));
}
