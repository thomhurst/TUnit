namespace TUnit.Assertions.AssertConditions.Connectors;

public class AndHas<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertConditions;

    public AndHas(BaseAssertCondition<TActual, TExpected> otherAssertConditions)
    {
        _otherAssertConditions = otherAssertConditions;
    }
}