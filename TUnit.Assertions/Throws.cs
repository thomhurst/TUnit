using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public class Throws<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; }

    public Throws(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder
            .AppendConnector(connectorType)
            .AppendExpression("Throws");
    }
    
    public ThrowsException<TActual, TAnd, TOr> Exception() => new(AssertionBuilder, ConnectorType, OtherAssertCondition, exception => exception);

    public BaseAssertCondition<TActual, TAnd, TOr> Nothing() => Wrap(new ThrowsNothingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(string.Empty)));


}