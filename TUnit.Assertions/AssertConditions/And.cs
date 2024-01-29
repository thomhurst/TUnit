using TUnit.Assertions.AssertConditions.ConditionEntries.Static;

namespace TUnit.Assertions.AssertConditions;

public class And<TExpected>
{
    private readonly IReadOnlyCollection<AssertCondition<TExpected>> _assertConditions;

    public And(IReadOnlyCollection<AssertCondition<TExpected>> assertConditions)
    {
        _assertConditions = assertConditions;
    }

    public AssertCondition<TExpected> EqualTo(TExpected expected)
    {
        return Is.EqualTo(_assertConditions, expected);
    }
}

public class And<TActual, TExpected>
{
    private readonly IReadOnlyCollection<ExpectedValueAssertCondition<TActual, TExpected>> _assertConditions;

    public And(IReadOnlyCollection<ExpectedValueAssertCondition<TActual, TExpected>> assertConditions)
    {
        _assertConditions = assertConditions;
    }
}