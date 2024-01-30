using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.AssertConditions.Connectors;

public sealed class DelegateAssertConditionAnd<TActual> : DelegateAssertCondition<TActual>
{
    private readonly DelegateAssertCondition<TActual> _condition1;
    private readonly DelegateAssertCondition<TActual> _condition2;

    public DelegateAssertConditionAnd(DelegateAssertCondition<TActual> condition1, DelegateAssertCondition<TActual> condition2)
    {
        _condition1 = condition1;
        _condition2 = condition2;
    }

    public override string DefaultMessage =>
        !_condition1.Passes(ActualValue, Exception) ? _condition1.Message :
        !_condition2.Passes(ActualValue, Exception) ? _condition2.Message : string.Empty;

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Assert((actualValue, exception)) && _condition2.Assert((actualValue, exception));
    }
}