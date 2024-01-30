namespace TUnit.Assertions.AssertConditions.Connectors;

public class OrHas<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertConditions;

    public OrHas(BaseAssertCondition<TActual, TExpected> otherAssertConditions)
    {
        _otherAssertConditions = otherAssertConditions;
    }
}