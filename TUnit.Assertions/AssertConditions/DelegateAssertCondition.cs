namespace TUnit.Assertions.AssertConditions;

public class DelegateAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    private readonly Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected>, bool> _condition;
    private readonly Func<TActual?, Exception?, string?, string> _defaultMessageFactory;

    public DelegateAssertCondition(TExpected? expected, 
        Func<TActual?, TExpected?, Exception?, DelegateAssertCondition<TActual, TExpected>, bool> condition,
        Func<TActual?, Exception?, string?, string> defaultMessageFactory) : base(expected)
    {
        _condition = condition;
        _defaultMessageFactory = defaultMessageFactory;
    }

    protected internal override string GetFailureMessage() =>
        _defaultMessageFactory(ActualValue, Exception, ActualExpression);

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition(actualValue, ExpectedValue, exception, this);
    }
}