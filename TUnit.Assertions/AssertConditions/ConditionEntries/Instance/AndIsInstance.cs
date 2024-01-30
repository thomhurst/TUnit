using TUnit.Assertions.AssertConditions.Combiners;
using TUnit.Assertions.AssertConditions.ConditionEntries.Static;

namespace TUnit.Assertions.AssertConditions.ConditionEntries.Instance;

public class AndIsInstance<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertCondition;

    public AndIsInstance(BaseAssertCondition<TActual, TExpected> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AssertConditionAnd<TActual, TExpected> EqualTo(TExpected expected)
    {
        return new AssertConditionAnd<TActual, TExpected>(_otherAssertCondition, Is.EqualTo<TActual, TExpected>(expected));
    }
}