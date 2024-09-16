using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public class AssertionConditionCombiner
{
    /// <summary>
    /// This method is responsible for combining assert conditions with other assert conditions
    /// if we are in the context of using `And` / `Or` operators.
    /// This should be called to wrap every condition to ensure they operate correctly with and/or conditions
    /// </summary>
    /// <param name="connector"></param>
    /// <param name="assertCondition"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static BaseAssertCondition<TActual> Combine<TActual, TAnd, TOr>(Connector<TActual, TAnd, TOr> connector, BaseAssertCondition<TActual> assertCondition)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return Combine(connector.OtherAssertCondition, connector.ChainType, assertCondition);
    }
    
    public static BaseAssertCondition<TActual> Combine<TActual, TAnd, TOr>(BaseAssertCondition<TActual>? initialAssertCondition, ChainType? connectorType, BaseAssertCondition<TActual> assertConditionToAppend)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        if (connectorType is null or ChainType.None)
        {
            assertConditionToAppend.IsWrapped = true;
            return assertConditionToAppend;
        }
        
        if (connectorType == ChainType.And)
        {
            return new AssertConditionAnd<TActual, TAnd, TOr>(initialAssertCondition!, assertConditionToAppend);
        }

        if (connectorType == ChainType.Or)
        {
            return new AssertConditionOr<TActual, TAnd, TOr>(initialAssertCondition!, assertConditionToAppend);
        }

        throw new ArgumentOutOfRangeException(nameof(connectorType), connectorType, "Unknown connector type");
    }
}