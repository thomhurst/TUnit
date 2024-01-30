using TUnit.Assertions.AssertConditions.ConditionEntries.Instance;

namespace TUnit.Assertions.AssertConditions;

public class Or<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertCondition;

    public Or(BaseAssertCondition<TActual, TExpected> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public OrIsInstance<TActual, TExpected> Is => new(_otherAssertCondition);
}