using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertConditions.Generic;

public class Property<TActual> : Property<TActual, object>
{
    public Property(string name, ConnectorType connectorType, BaseAssertCondition<TActual,object> otherAssertConditions) 
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
    private readonly BaseAssertCondition<TActual, TExpected>? _otherAssertConditions;

    public Property(string name, ConnectorType connectorType, BaseAssertCondition<TActual,TExpected> otherAssertConditions) 
        : this(name)
    {
        _connectorType = connectorType;
        _otherAssertConditions = otherAssertConditions;
    }

    public BaseAssertCondition<TActual, TExpected> EqualTo(TExpected expected)
    {
        var assertCondition = new PropertyEqualsAssertCondition<TActual, TExpected>(name, expected);

        if (_connectorType is ConnectorType.And)
        {
            return new AssertConditionAnd<TActual, TExpected>(_otherAssertConditions!, assertCondition);
        }
        
        if (_connectorType is ConnectorType.Or)
        {
            return new AssertConditionOr<TActual, TExpected>(_otherAssertConditions!, assertCondition);
        }

        return assertCondition;
    }
}