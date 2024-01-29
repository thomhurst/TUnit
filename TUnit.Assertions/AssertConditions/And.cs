namespace TUnit.Assertions.AssertConditions;

public class And<TActual>
{
    private readonly IReadOnlyCollection<AssertCondition<TActual>> _assertConditions;

    public And(IReadOnlyCollection<AssertCondition<TActual>> assertConditions)
    {
        _assertConditions = assertConditions;
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