namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected> : BaseAssertCondition<TActual>
{
    internal TExpected? ExpectedValue { get; }
    
    internal AssertCondition(AssertionBuilder<TActual> assertionBuilder, TExpected? expected) : base(assertionBuilder)
    {
        ExpectedValue = expected;
    }
    
    private Func<TActual?, TExpected?, Exception?, string>? MessageFactory { get; set; }

    protected internal override string Message => MessageFactory?.Invoke(ActualValue, ExpectedValue, Exception) ?? DefaultMessage;
    
    public AssertCondition<TActual, TExpected> WithMessage(Func<TActual?, TExpected?, Exception?, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}