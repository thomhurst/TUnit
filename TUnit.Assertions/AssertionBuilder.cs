namespace TUnit.Assertions;

public abstract class AssertionBuilder<TActual>
{
    private readonly string? _callerExpression;

    public AssertionBuilder(string? callerExpression)
    {
        _callerExpression = callerExpression;
    }

    internal string GetCallerExpressionPrefix()
    {
        if (string.IsNullOrEmpty(_callerExpression))
        {
            return string.Empty;
        }

        return $"{_callerExpression}: ";
    }
    
    protected internal abstract Task<AssertionData<TActual>> GetAssertionData();
}