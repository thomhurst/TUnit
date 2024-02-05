using System.ComponentModel;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public abstract class Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public ConnectorType ConnectorType { get; }
    
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public BaseAssertCondition<TActual, TAnd, TOr>? OtherAssertCondition { get; }

    public Connector(ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition)
    {
        ConnectorType = connectorType;
        OtherAssertCondition = otherAssertCondition;
    }

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
    
    public AssertCondition<TActual, TExpected, TAnd, TOr> Wrap<TExpected, TAssertCondition>(TAssertCondition assertCondition) 
        where TAssertCondition : AssertCondition<TActual, TExpected, TAnd, TOr>
    {
        var castOtherAssertCondition = (AssertCondition<TActual, TExpected, TAnd, TOr>)OtherAssertCondition!;
        
        return ConnectorType switch
        {
            ConnectorType.None => assertCondition,
            ConnectorType.And => new AssertConditionAnd<TActual, TExpected, TAnd, TOr>(castOtherAssertCondition, assertCondition),
            ConnectorType.Or => new AssertConditionOr<TActual, TExpected, TAnd, TOr>(castOtherAssertCondition, assertCondition),
            _ => throw new ArgumentOutOfRangeException(nameof(ConnectorType), ConnectorType, "Unknown connector type")
        };
    }
}