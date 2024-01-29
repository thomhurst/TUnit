namespace TUnit.Assertions.AssertConditions;

public class Or<TActual, TExpected>
{
    private readonly IReadOnlyCollection<AssertCondition<TActual, TExpected>> _assertConditions;

    public Or(IReadOnlyCollection<AssertCondition<TActual, TExpected>> assertConditions)
    {
        _assertConditions = assertConditions;
    }
}