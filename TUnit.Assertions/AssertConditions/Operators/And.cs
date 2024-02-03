using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertConditions.Operators;

public class And<TActual>
{
    private readonly BaseAssertCondition<TActual> _otherAssertCondition;

    public And(BaseAssertCondition<TActual> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public Is<TActual> Is => new(_otherAssertCondition.AssertionBuilder, ConnectorType.And, _otherAssertCondition);
    public Has<TActual> Has => new(_otherAssertCondition.AssertionBuilder, ConnectorType.And, _otherAssertCondition);
}