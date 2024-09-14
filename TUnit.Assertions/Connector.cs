using System.ComponentModel;
using System.Diagnostics;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public abstract class Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
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
    protected BaseAssertCondition<TActual, TAnd, TOr> Combine(BaseAssertCondition<TActual, TAnd, TOr> assertCondition)
    {
        return AssertionConditionCombiner.Combine(OtherAssertCondition, ConnectorType, assertCondition);
    }

    [Obsolete("This is a base `object` method that should not be called.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerHidden]
    public new void Equals(object? obj)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }

    [Obsolete("This is a base `object` method that should not be called.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerHidden]
    public new void ReferenceEquals(object a, object b)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }
}