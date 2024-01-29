using TUnit.Assertions.AssertConditions.Combiners;
using TUnit.Assertions.AssertConditions.ConditionEntries.Static;

namespace TUnit.Assertions.AssertConditions;

public class Or<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertCondition;

    public Or(BaseAssertCondition<TActual, TExpected> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AssertConditionOr<TActual, TExpected> EqualTo(TExpected expected)
    {
        return new AssertConditionOr<TActual, TExpected>(_otherAssertCondition, Is.EqualTo<TActual, TExpected>(expected));
    }
}