namespace TUnit.Assertions;

public abstract class AssertionBuilder<TActual>
{
    public string? CallerExpression { get; }

    public AssertionBuilder(string? callerExpression)
    {
        CallerExpression = callerExpression;
    }
    
    protected internal abstract Task<AssertionData<TActual>> GetAssertionData();
}