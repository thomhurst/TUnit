using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions;

public class Has<TRootObject, TAnd, TOr> : Connector<TRootObject, TAnd, TOr>
    where TAnd : IAnd<TRootObject, TAnd, TOr>
    where TOr : IOr<TRootObject, TAnd, TOr>
{
    protected internal AssertionBuilder<TRootObject, TAnd, TOr> AssertionBuilder { get; }

    public Has(AssertionBuilder<TRootObject, TAnd, TOr> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TRootObject, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder
            .AppendConnector(connectorType);
    }
}