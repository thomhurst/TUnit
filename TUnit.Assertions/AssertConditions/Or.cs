namespace TUnit.Assertions.AssertConditions;

public class Or<TExpected>
{
    private readonly IReadOnlyCollection<AssertCondition<TExpected>> _assertConditions;

    public Or(IReadOnlyCollection<AssertCondition<TExpected>> assertConditions)
    {
        _assertConditions = assertConditions;
    }
}

public class Or<TActual, TExpected>
{
    private readonly IReadOnlyCollection<ExpectedValueAssertCondition<TActual, TExpected>> _assertConditions;

    public Or(IReadOnlyCollection<ExpectedValueAssertCondition<TActual, TExpected>> assertConditions)
    {
        _assertConditions = assertConditions;
    }
}