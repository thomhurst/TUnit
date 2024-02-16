using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions;

public class DelegateAssertCondition<TActual, TExpected, TAnd, TOr> : AssertCondition<TActual, TExpected, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected, TAnd, TOr>, bool> _condition;

    public DelegateAssertCondition(AssertionBuilder<TActual> assertionBuilder, 
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