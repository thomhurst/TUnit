namespace TUnit.Assertions;

public abstract class ExpectedValueAssertCondition<TActual, TExpected> : AssertCondition<TActual>
{
    internal TExpected ExpectedValue { get; }

    internal ExpectedValueAssertCondition(TExpected expected)
    {
        ExpectedValue = expected;
    }

    public new string Message => MessageFactory?.Invoke((ExpectedValue, ActualValue)) ?? DefaultMessage;

    private Func<(TExpected ExpectedValue, TActual ActualValue), string>? MessageFactory { get; set; }
    
    public IAssertCondition<TActual> WithMessage(Func<(TExpected ExpectedValue, TActual ActualValue), string> messageFactory)
    {
        MessageFactory = messageFactory!;
        return this;
    }
}