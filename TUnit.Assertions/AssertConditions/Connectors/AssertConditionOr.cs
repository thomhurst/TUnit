using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Connectors;

public sealed class AssertConditionOr<TActual, TExpected, TAnd, TOr> : AssertCondition<TActual, TExpected, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly AssertCondition<TActual,TExpected,TAnd,TOr> _condition1;
    private readonly AssertCondition<TActual,TExpected,TAnd,TOr> _condition2;

    public AssertConditionOr(AssertCondition<TActual, TExpected, TAnd, TOr> condition1, AssertCondition<TActual, TExpected, TAnd, TOr> condition2) : base(condition1.AssertionBuilder, condition1.ExpectedValue)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);

        _condition1 = condition1;
        _condition2 = condition2;
    }

    protected override string DefaultMessage => string.Empty;
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Assert(actualValue, exception) || _condition2.Assert(actualValue, exception);
    }
}

public class AssertConditionOr<TActual, TAnd, TOr> : BaseAssertCondition<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly BaseAssertCondition<TActual, TAnd, TOr> _condition1;
    private readonly BaseAssertCondition<TActual, TAnd, TOr> _condition2;

    public AssertConditionOr(BaseAssertCondition<TActual, TAnd, TOr> condition1, BaseAssertCondition<TActual, TAnd, TOr> condition2) : base(condition1.AssertionBuilder)
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