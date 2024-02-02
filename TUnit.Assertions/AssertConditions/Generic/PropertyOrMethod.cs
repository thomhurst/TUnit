using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertConditions.Generic;

public class PropertyOrMethod<TActual> : Property<TActual, object>
{
    public PropertyOrMethod(string name, ConnectorType connectorType, BaseAssertCondition<TActual> otherAssertConditions) 
        : base(name, connectorType, otherAssertConditions)
    {
    }

    public PropertyOrMethod(AssertionBuilder<TActual> assertionBuilder, string name) : base(assertionBuilder, name)
    {
    }
}

public class PropertyOrMethod<TActual, TExpected>(AssertionBuilder<TActual> assertionBuilder, string name)
{
    private readonly ConnectorType? _connectorType;
    private readonly BaseAssertCondition<TActual>? _otherAssertConditions;

    public PropertyOrMethod(string name, ConnectorType connectorType, BaseAssertCondition<TActual> otherAssertConditions) 
        : this(otherAssertConditions.AssertionBuilder, name)
    {
        _connectorType = connectorType;
        _otherAssertConditions = otherAssertConditions;
    }

    public BaseAssertCondition<TActual> EqualTo(TExpected expected)
    {
        var assertCondition = new PropertyEqualsAssertCondition<TActual, TExpected>(assertionBuilder, name, expected);

        if (_connectorType is ConnectorType.And)
        {
            return new AssertConditionAnd<TActual>(_otherAssertConditions!, assertCondition);
        }
        
        if (_connectorType is ConnectorType.Or)
        {
            return new AssertConditionOr<TActual>(_otherAssertConditions!, assertCondition);
        }

        return assertCondition;
    }
}