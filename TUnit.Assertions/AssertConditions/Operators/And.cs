using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertConditions.Operators;

public class And<TActual>
{
    private readonly BaseAssertCondition<TActual> _otherAssertCondition;

    public And(BaseAssertCondition<TActual> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AndIs<TActual> Is => new(_otherAssertCondition);
    public AndHas<TActual> Has => new(_otherAssertCondition);
}