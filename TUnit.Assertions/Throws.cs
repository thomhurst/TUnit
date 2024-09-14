using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions;

public class Throws<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }

    public Throws(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder
            .AppendConnector(connectorType)
            .AppendExpression("Throws");
    }
    
    public ThrowsException<TActual, TAnd, TOr> Exception() => new(AssertionBuilder, ConnectorType, OtherAssertCondition, exception => exception);

    public BaseAssertCondition<TActual, TAnd, TOr> Nothing() => Combine(new ThrowsNothingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(string.Empty)));


}