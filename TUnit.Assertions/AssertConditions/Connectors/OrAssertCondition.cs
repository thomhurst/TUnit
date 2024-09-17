namespace TUnit.Assertions.AssertConditions.Connectors;

internal class OrAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    private readonly BaseAssertCondition<TActual> _condition1;
    private readonly BaseAssertCondition<TActual> _condition2;

    public OrAssertCondition(BaseAssertCondition<TActual> condition1, BaseAssertCondition<TActual> condition2)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);
        
        _condition1 = condition1;
        _condition2 = condition2;
    }

    protected internal override string Message => $"{_condition1.Message}{Environment.NewLine} or{Environment.NewLine}{_condition2.Message}";
    protected override string DefaultMessage => string.Empty;

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        return _condition1.Assert(actualValue, exception, rawValueExpression) || _condition2.Assert(actualValue, exception, rawValueExpression);
    }
}