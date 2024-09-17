using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions;

public class DelegateAssertCondition<TActual, TExpected, TAnd, TOr> : AssertCondition<TActual, TExpected, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected, TAnd, TOr>, bool> _condition;

    public DelegateAssertCondition(TExpected? expected, 
        Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected, TAnd, TOr>, bool> condition,
        Func<TActual?, Exception?, string?, string> defaultMessageFactory) : base(expected)
    {
        _condition = condition;
        WithMessage(defaultMessageFactory);
    }

    protected override string DefaultMessage => string.Empty;

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        return _condition(actualValue, ExpectedValue, exception, this);
    }
}