using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertConditions.Connectors;

public class AndHas<TActual>
{
    private readonly BaseAssertCondition<TActual> _otherAssertConditions;

    public AndHas(BaseAssertCondition<TActual> otherAssertConditions)
    {
        _otherAssertConditions = otherAssertConditions;
    }
    
    public Property<TActual, int> Count => new("Count", ConnectorType.And, _otherAssertConditions);
    public Property<TActual, int> Length => new("Length", ConnectorType.And, _otherAssertConditions);
    public Property<TActual> Value => new("Value", ConnectorType.And, _otherAssertConditions);
    
    public Property<TActual> Property(string name) => new(name, ConnectorType.And, _otherAssertConditions);
    public Property<TActual, T> Property<T>(string name) => new(name, ConnectorType.And, _otherAssertConditions);
}