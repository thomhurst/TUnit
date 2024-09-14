using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions;

public class DelegateAssertCondition<TActual, TExpected, TAnd, TOr> : AssertCondition<TActual, TExpected, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected, TAnd, TOr>, bool> _condition;

    public DelegateAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, 
        TExpected? expected, 
        Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected, TAnd, TOr>, bool> condition,
        Func<TActual?, Exception?, string> defaultMessageFactory) : base(assertionBuilder, expected)
    {
        _condition = condition;
        WithMessage(defaultMessageFactory);
    }

    protected override string DefaultMessage => string.Empty;

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition(actualValue, ExpectedValue, exception, this);
    }
}