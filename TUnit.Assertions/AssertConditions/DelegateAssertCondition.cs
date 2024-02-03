namespace TUnit.Assertions.AssertConditions;

public class DelegateAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    private readonly Func<TActual?, TExpected?, Exception?, bool> _condition;

    public DelegateAssertCondition(AssertionBuilder<TActual> assertionBuilder, 
        TExpected? expected, 
        Func<TActual?, TExpected?, Exception?, bool> condition,
        Func<TActual?, TExpected?, Exception?, string> messageFactory) : base(assertionBuilder, expected)
    {
        _condition = condition;
        WithMessage(messageFactory);
    }

    protected override string DefaultMessage { get; }
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition(actualValue, ExpectedValue, exception);
    }
}