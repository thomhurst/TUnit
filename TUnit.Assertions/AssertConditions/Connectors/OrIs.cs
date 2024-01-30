namespace TUnit.Assertions.AssertConditions.Connectors;

public class OrIs<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertCondition;

    public OrIs(BaseAssertCondition<TActual, TExpected> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AssertConditionOr<TActual, TExpected> EqualTo(TExpected expected)
    {
        return new AssertConditionOr<TActual, TExpected>(_otherAssertCondition, Is.EqualTo<TActual, TExpected>(expected));
    }
}