namespace TUnit.Assertions.AssertConditions.Connectors;

public class AndIs<TActual>
{
    private readonly BaseAssertCondition<TActual> _otherAssertCondition;

    public AndIs(BaseAssertCondition<TActual> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AssertConditionAnd<TActual> EqualTo<TExpected>(TExpected expected)
    {
        return new AssertConditionAnd<TActual>(_otherAssertCondition, Is.EqualTo<TActual, TExpected>(expected));
    }
}