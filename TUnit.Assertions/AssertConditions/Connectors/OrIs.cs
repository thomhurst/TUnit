namespace TUnit.Assertions.AssertConditions.Connectors;

public class OrIs<TActual>
{
    private readonly BaseAssertCondition<TActual> _otherAssertCondition;

    public OrIs(BaseAssertCondition<TActual> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AssertConditionOr<TActual> EqualTo<TExpected>(TExpected expected)
    {
        return new AssertConditionOr<TActual>(_otherAssertCondition, Is.EqualTo<TActual, TExpected>(expected));
    }
}