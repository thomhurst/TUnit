using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertConditions.Operators;

public class Or<TActual>
{
    private readonly BaseAssertCondition<TActual> _otherAssertCondition;

    public Or(BaseAssertCondition<TActual> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public OrIs<TActual> Is => new(_otherAssertCondition);
    public OrHas<TActual> Has => new(_otherAssertCondition);
}