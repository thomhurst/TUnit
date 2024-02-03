using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertConditions.Connectors;

public class AndHas<TActual>
{
    internal BaseAssertCondition<TActual> OtherAssertCondition { get; }

    public AndHas(BaseAssertCondition<TActual> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
    
    public Property<TActual> Property(string name) => new(name, ConnectorType.And, OtherAssertCondition);
    public Property<TActual, T> Property<T>(string name) => new(name, ConnectorType.And, OtherAssertCondition);
}