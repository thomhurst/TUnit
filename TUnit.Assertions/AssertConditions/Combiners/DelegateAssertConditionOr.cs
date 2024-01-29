using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.AssertConditions.Combiners;

public class DelegateAssertConditionOr<TActual> : DelegateAssertCondition<TActual>
{
    private readonly DelegateAssertCondition<TActual> _condition1;
    private readonly DelegateAssertCondition<TActual> _condition2;

    public DelegateAssertConditionOr(DelegateAssertCondition<TActual> condition1, DelegateAssertCondition<TActual> condition2)
    {
        _condition1 = condition1;
        _condition2 = condition2;
    }

    public override string DefaultMessage => $"{_condition1.DefaultMessage} & {_condition2.DefaultMessage}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Passes(actualValue, exception) || _condition2.Passes(actualValue, exception);
    }
}