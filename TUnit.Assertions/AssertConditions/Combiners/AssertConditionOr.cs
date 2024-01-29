namespace TUnit.Assertions.AssertConditions.Combiners;

public class AssertConditionOr<TActual, TExpected> : BaseAssertCondition<TActual, TExpected>
{
    private readonly BaseAssertCondition<TActual, TExpected> _condition1;
    private readonly BaseAssertCondition<TActual, TExpected> _condition2;

    public AssertConditionOr(BaseAssertCondition<TActual, TExpected> condition1, BaseAssertCondition<TActual, TExpected> condition2)
    {
        _condition1 = condition1;
        _condition2 = condition2;
    }

    public override string DefaultMessage => $"{_condition1.DefaultMessage} & {_condition2.DefaultMessage}";

    protected internal override bool Passes(TActual actualValue)
    {
        ActualValue = actualValue;
        return _condition1.Passes(actualValue) || _condition2.Passes(actualValue);
    }
}