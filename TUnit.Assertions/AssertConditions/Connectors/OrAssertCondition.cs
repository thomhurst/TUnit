using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Connectors;

internal class OrAssertCondition<TActual, TAnd, TOr> : BaseAssertCondition<TActual>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly BaseAssertCondition<TActual> _condition1;
    private readonly BaseAssertCondition<TActual> _condition2;

    public OrAssertCondition(BaseAssertCondition<TActual> condition1, BaseAssertCondition<TActual> condition2) : base(condition1.AssertionBuilder)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);
        
        _condition1 = condition1;
        _condition2 = condition2;
    }

    protected internal override string Message => $"{_condition1.Message} &{_condition2.Message.Replace(_condition2.AssertionBuilder.ExpressionBuilder?.ToString() ?? string.Empty, string.Empty)}";
    protected override string DefaultMessage => string.Empty;

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Assert(actualValue, exception) || _condition2.Assert(actualValue, exception);
    }
}