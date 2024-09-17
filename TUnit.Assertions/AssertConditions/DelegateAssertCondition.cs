namespace TUnit.Assertions.AssertConditions;

public class DelegateAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    private readonly Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected>, bool> _condition;

    public DelegateAssertCondition(TExpected? expected, 
        Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected>, bool> condition,
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