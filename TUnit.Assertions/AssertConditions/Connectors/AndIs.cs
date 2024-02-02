using TUnit.Assertions.AssertConditions.Generic;

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
        return new AssertConditionAnd<TActual>(OtherAssertCondition, new EqualsAssertCondition<TActual, TExpected>(OtherAssertCondition.AssertionBuilder,  expected));
    }
}