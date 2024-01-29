namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected>
{
    internal readonly IReadOnlyCollection<AssertCondition<TActual, TExpected>> AllAssertConditions;
    internal TExpected? ExpectedValue { get; }
    
    internal AssertCondition(IReadOnlyCollection<AssertCondition<TActual, TExpected>> previousAssertConditions, TExpected? expected)
    {
        IReadOnlyCollection<AssertCondition<TActual, TExpected>> conditionsUntilNow = [..previousAssertConditions, this];
        AllAssertConditions = conditionsUntilNow;

        ExpectedValue = expected;
        
        And = new And<TActual, TExpected>(conditionsUntilNow);
        Or = new Or<TActual, TExpected>(conditionsUntilNow);
    }

    private Func<TActual, string>? MessageFactory { get; set; }

    protected TActual ActualValue { get; private set; } = default!;

    public bool Assert(TActual actualValue)
    {
        ActualValue = actualValue;
        return Passes(actualValue);
    }

    public abstract string DefaultMessage { get; }

    protected abstract bool Passes(TActual actualValue);

    public string Message => MessageFactory?.Invoke(ActualValue) ?? DefaultMessage;
    
    public AssertCondition<TActual, TExpected> WithMessage(Func<TActual, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }

    public And<TActual, TExpected> And { get; }
    public Or<TActual, TExpected> Or { get; }
}