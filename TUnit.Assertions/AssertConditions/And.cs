using TUnit.Assertions.AssertConditions.Combiners;
using TUnit.Assertions.AssertConditions.ConditionEntries.Instance;
using TUnit.Assertions.AssertConditions.ConditionEntries.Static;

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