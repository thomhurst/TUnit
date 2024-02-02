namespace TUnit.Assertions.AssertConditions.Connectors;

public class OrIs<TActual>
{
    internal BaseAssertCondition<TActual> OtherAssertCondition { get; }

    public OrIs(BaseAssertCondition<TActual> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
    
    public AssertConditionOr<TActual> EqualTo<TExpected>(TExpected expected)
    {
        return new AssertConditionOr<TActual>(OtherAssertCondition, Is.EqualTo<TActual, TExpected>(expected));
    }
}