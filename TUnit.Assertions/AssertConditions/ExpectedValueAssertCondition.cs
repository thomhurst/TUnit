namespace TUnit.Assertions.AssertConditions;

public abstract class ExpectedValueAssertCondition<TActual, TExpected> : AssertCondition<TActual>
{
    internal TExpected ExpectedValue { get; }

    internal ExpectedValueAssertCondition(TExpected expected)
    {
        ExpectedValue = expected;
        And = new And<TActual, TExpected>([this]);
        Or = new Or<TActual, TExpected>([this]);
    }

    public new string Message => MessageFactory?.Invoke((ExpectedValue, ActualValue)) ?? DefaultMessage;

    private Func<(TExpected ExpectedValue, TActual ActualValue), string>? MessageFactory { get; set; }
    
    public AssertCondition<TActual> WithMessage(Func<(TExpected ExpectedValue, TActual ActualValue), string> messageFactory)
    {
        MessageFactory = messageFactory!;
        return this;
    }
    
    public new And<TActual, TExpected> And { get; }
    public new Or<TActual, TExpected> Or { get; }
}