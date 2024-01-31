using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertConditions.Connectors;

public class AndHas<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertConditions;

    public AndHas(BaseAssertCondition<TActual, TExpected> otherAssertConditions)
    {
        _otherAssertConditions = otherAssertConditions;
    }
    
    public Property<TActual, TExpected> Count => new("Count", ConnectorType.And, _otherAssertConditions);
    public Property<TActual, TExpected> Length => new("Length", ConnectorType.And, _otherAssertConditions);
    public Property<TActual, TExpected> Value => new("Value", ConnectorType.And, _otherAssertConditions);
    
    public Property<TActual, TExpected> Property(string name) => new(name, ConnectorType.And, _otherAssertConditions);
}