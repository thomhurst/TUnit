using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertConditions.Connectors;

public class AndHas<TActual>
{
    internal BaseAssertCondition<TActual> OtherAssertCondition { get; }

    public AndHas(BaseAssertCondition<TActual> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
    
    public Property<TActual, int> Count => new("Count", ConnectorType.And, OtherAssertCondition);
    public Property<TActual, int> Length => new("Length", ConnectorType.And, OtherAssertCondition);
    public Property<TActual> Value => new("Value", ConnectorType.And, OtherAssertCondition);
    
    public Property<TActual> Property(string name) => new(name, ConnectorType.And, OtherAssertCondition);
    public Property<TActual, T> Property<T>(string name) => new(name, ConnectorType.And, OtherAssertCondition);
}