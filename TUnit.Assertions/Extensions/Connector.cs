using System.ComponentModel;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

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
        if (ConnectorType == ConnectorType.None)
        {
            assertCondition.IsWrapped = true;
            return assertCondition;
        }

        if (ConnectorType == ConnectorType.And)
        {
            return new AssertConditionAnd<TActual, TAnd, TOr>(OtherAssertCondition!, assertCondition);
        }

        if (ConnectorType == ConnectorType.Or)
        {
            return new AssertConditionOr<TActual, TAnd, TOr>(OtherAssertCondition!, assertCondition);
        }

        throw new ArgumentOutOfRangeException(nameof(ConnectorType), ConnectorType, "Unknown connector type");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj)
    {
        // ReSharper disable once BaseObjectEqualsIsObjectEquals
        return base.Equals(obj);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
    {
        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        return base.GetHashCode();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string? ToString()
    {
        return base.ToString();
    }
}