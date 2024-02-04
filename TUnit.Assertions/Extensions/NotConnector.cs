using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public abstract class NotConnector<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected NotConnector(ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
    }

    protected BaseAssertCondition<TActual, TAnd, TOr> Invert(BaseAssertCondition<TActual, TAnd, TOr> assertCondition, Func<TActual?, Exception?, string> messageFactory)
    {
        return Wrap(assertCondition.Invert(messageFactory));
    }
}