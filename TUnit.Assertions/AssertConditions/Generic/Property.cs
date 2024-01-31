using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertConditions.Generic;

public class Property<TActual> : Property<TActual, object>
{
    public Property(string name, ConnectorType connectorType, BaseAssertCondition<TActual> otherAssertConditions) 
        : base(name, connectorType, otherAssertConditions)
    {
    }

    public Property(string name) : base(name)
    {
    }
}

public class Property<TActual, TExpected>(string name)
{
    private readonly ConnectorType? _connectorType;
    private readonly BaseAssertCondition<TActual>? _otherAssertConditions;

    public Property(string name, ConnectorType connectorType, BaseAssertCondition<TActual> otherAssertConditions) 
        : this(name)
    {
        _connectorType = connectorType;
        _otherAssertConditions = otherAssertConditions;
    }

    public BaseAssertCondition<TActual> EqualTo(TExpected expected)
    {
        var assertCondition = new PropertyEqualsAssertCondition<TActual, TExpected>(name, expected);

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