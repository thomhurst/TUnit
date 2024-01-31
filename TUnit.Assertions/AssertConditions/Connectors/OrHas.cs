namespace TUnit.Assertions.AssertConditions.Connectors;

public class OrHas<TActual>
{
    private readonly BaseAssertCondition<TActual> _otherAssertConditions;

    public OrHas(BaseAssertCondition<TActual> otherAssertConditions)
    {
        _otherAssertConditions = otherAssertConditions;
    }
}