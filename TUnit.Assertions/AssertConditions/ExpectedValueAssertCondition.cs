namespace TUnit.Assertions.AssertConditions;

public abstract class ExpectedValueAssertCondition<TActual, TExpected> : AssertCondition<TActual>
{
    internal readonly IReadOnlyCollection<ExpectedValueAssertCondition<TActual, TExpected>> PreviousConditions;
    internal TExpected ExpectedValue { get; }
    
    internal ExpectedValueAssertCondition(IReadOnlyCollection<ExpectedValueAssertCondition<TActual, TExpected>> previousConditions, TExpected expected) : base(previousConditions)
    {
        IReadOnlyCollection<ExpectedValueAssertCondition<TActual, TExpected>> conditionsUntilNow = [..previousConditions, this];
        PreviousConditions = conditionsUntilNow;
        ExpectedValue = expected;
        And = new And<TActual, TExpected>(conditionsUntilNow);
        Or = new Or<TActual, TExpected>(conditionsUntilNow);
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