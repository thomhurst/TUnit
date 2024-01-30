using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertConditions.Operators;

public class Or<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _otherAssertCondition;

    public Or(BaseAssertCondition<TActual, TExpected> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public OrIs<TActual, TExpected> Is => new(_otherAssertCondition);
    public OrHas<TActual, TExpected> Has => new(_otherAssertCondition);
}