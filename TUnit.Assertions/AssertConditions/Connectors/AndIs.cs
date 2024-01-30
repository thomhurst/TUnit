namespace TUnit.Assertions.AssertConditions.Connectors;

public class AndIs<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertCondition;

    public AndIs(BaseAssertCondition<TActual, TExpected> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AssertConditionAnd<TActual, TExpected> EqualTo(TExpected expected)
    {
        return new AssertConditionAnd<TActual, TExpected>(_otherAssertCondition, Is.EqualTo<TActual, TExpected>(expected));
    }
}