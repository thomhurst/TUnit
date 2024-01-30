using TUnit.Assertions.AssertConditions.ConditionEntries.Instance;

namespace TUnit.Assertions.AssertConditions;

public class And<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertCondition;

    public And(BaseAssertCondition<TActual, TExpected> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AndIsInstance<TActual, TExpected> Is => new(_otherAssertCondition);
}