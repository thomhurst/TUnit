using System.ComponentModel;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public abstract class Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected internal ConnectorType ConnectorType { get; }
    
    protected internal BaseAssertCondition<TActual, TAnd, TOr>? OtherAssertCondition { get; }

    protected Connector(ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition)
    {
        ConnectorType = connectorType;
        OtherAssertCondition = otherAssertCondition;
    }

    /// <summary>
    /// This method is responsible for combining assert conditions with other assert conditions
    /// if we are in the context of using `And` / `Or` operators.
    /// This should be called to wrap every condition to ensure they operate correctly with and/or conditions
    /// </summary>
    /// <param name="assertCondition"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public BaseAssertCondition<TActual, TAnd, TOr> Wrap(BaseAssertCondition<TActual, TAnd, TOr> assertCondition)
    {
        return ConnectorType switch
        {
            ConnectorType.None => assertCondition,
            ConnectorType.And => new AssertConditionAnd<TActual, TAnd, TOr>(OtherAssertCondition!, assertCondition),
            ConnectorType.Or => new AssertConditionOr<TActual, TAnd, TOr>(OtherAssertCondition!, assertCondition),
            _ => throw new ArgumentOutOfRangeException(nameof(ConnectorType), ConnectorType, "Unknown connector type")
        };
    }
}