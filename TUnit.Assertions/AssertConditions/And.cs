using TUnit.Assertions.AssertConditions.ConditionEntries.Static;

namespace TUnit.Assertions.AssertConditions;

public class And<TActual, TExpected>
{
    private readonly IReadOnlyCollection<AssertCondition<TActual, TExpected>> _assertConditions;

    public And(IReadOnlyCollection<AssertCondition<TActual, TExpected>> assertConditions)
    {
        _assertConditions = assertConditions;
    }
    
    public AssertCondition<TActual, TExpected> EqualTo(TExpected expected)
    {
        return Is.EqualTo(_assertConditions, expected);
    }
}