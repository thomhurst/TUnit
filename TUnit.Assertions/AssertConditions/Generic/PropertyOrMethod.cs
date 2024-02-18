using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Generic;

public class PropertyOrMethod<TActual, TAnd, TOr> : Property<TActual, object, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public PropertyOrMethod(string name, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr> otherAssertConditions) 
        : base(name, connectorType, otherAssertConditions)
    {
    }

    public PropertyOrMethod(AssertionBuilder<TActual> assertionBuilder, string name) : base(assertionBuilder, name)
    {
    }
}

public class PropertyOrMethod<TActual, TExpected, TAnd, TOr>(AssertionBuilder<TActual> assertionBuilder, string name)
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly ConnectorType? _connectorType;
    private readonly BaseAssertCondition<TActual, TAnd, TOr>? _otherAssertConditions;

    public PropertyOrMethod(string name, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr> otherAssertConditions) 
        : this(otherAssertConditions.AssertionBuilder, name)
    {
        _connectorType = connectorType;
        _otherAssertConditions = otherAssertConditions;
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(TExpected expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        var assertCondition = new PropertyEqualsAssertCondition<TActual, TExpected, TAnd, TOr>(assertionBuilder, name, expected);

        if (_connectorType is ConnectorType.And)
        {
            return new AssertConditionAnd<TActual, TAnd, TOr>(_otherAssertConditions!, assertCondition);
        }
        
        if (_connectorType is ConnectorType.Or)
        {
            return new AssertConditionOr<TActual, TAnd, TOr>(_otherAssertConditions!, assertCondition);
        }

        return assertCondition;
    }
}