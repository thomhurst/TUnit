namespace TUnit.Assertions.AssertConditions;

public class AsyncAnd<TActual>
{
    private readonly AsyncAssertCondition<TActual> _assertCondition;

    public AsyncAnd(AsyncAssertCondition<TActual> assertCondition)
    {
        _assertCondition = assertCondition;
    }
}

public class AsyncAnd
{
    private readonly AsyncAssertCondition _assertCondition;

    public AsyncAnd(AsyncAssertCondition assertCondition)
    {
        _assertCondition = assertCondition;
    }
}