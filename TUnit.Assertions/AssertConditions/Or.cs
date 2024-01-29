namespace TUnit.Assertions.AssertConditions;

public class Or<TActual>
{
    private readonly IReadOnlyCollection<AssertCondition<TActual>> _assertConditions;

    public Or(IReadOnlyCollection<AssertCondition<TActual>> assertConditions)
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