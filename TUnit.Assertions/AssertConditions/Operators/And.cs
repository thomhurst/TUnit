using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertConditions.Operators;

public class And<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertCondition;

    public And(BaseAssertCondition<TActual, TExpected> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public AndIs<TActual, TExpected> Is => new(_otherAssertCondition);
    public AndHas<TActual, TExpected> Has => new(_otherAssertCondition);
}