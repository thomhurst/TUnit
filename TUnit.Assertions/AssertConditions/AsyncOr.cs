namespace TUnit.Assertions.AssertConditions;

public class AsyncOr<TActual>
{
    private readonly AsyncAssertCondition<TActual> _assertCondition;

    public AsyncOr(AsyncAssertCondition<TActual> assertCondition)
    {
        _assertCondition = assertCondition;
    }
}

public class AsyncOr
{
    private readonly AsyncAssertCondition _assertCondition;

    public AsyncOr(AsyncAssertCondition assertCondition)
    {
        _assertCondition = assertCondition;
    }
}