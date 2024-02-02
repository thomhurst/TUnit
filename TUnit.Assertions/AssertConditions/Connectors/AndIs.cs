namespace TUnit.Assertions.AssertConditions.Connectors;

public class AndIs<TActual>
{
    internal BaseAssertCondition<TActual> OtherAssertCondition { get; }

    public AndIs(BaseAssertCondition<TActual> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
    
    public AssertConditionAnd<TActual> EqualTo<TExpected>(TExpected expected)
    {
        return new AssertConditionAnd<TActual>(OtherAssertCondition, Is.EqualTo<TActual, TExpected>(expected));
    }
}