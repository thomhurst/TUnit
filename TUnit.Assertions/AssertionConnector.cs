using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions;

public class AssertionConnector<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }

    public AssertionConnector(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ChainType chainType) : base(chainType)
    {
        AssertionBuilder = assertionBuilder
            .AppendConnector(chainType);
    }
}