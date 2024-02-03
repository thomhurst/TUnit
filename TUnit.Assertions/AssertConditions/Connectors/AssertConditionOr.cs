namespace TUnit.Assertions.AssertConditions.Connectors;

public sealed class AssertConditionOr<TActual> : BaseAssertCondition<TActual>
{
    private readonly BaseAssertCondition<TActual> _condition1;
    private readonly BaseAssertCondition<TActual> _condition2;

    public AssertConditionOr(BaseAssertCondition<TActual> condition1, BaseAssertCondition<TActual> condition2) : base(condition1.AssertionBuilder)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);

        _condition1 = condition1;
        _condition2 = condition2;
    }

    protected internal override string Message => $"{_condition1.Message} & {_condition2.Message}";
    protected override string DefaultMessage => string.Empty;

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Assert(actualValue, exception) || _condition2.Assert(actualValue, exception);
    }
}