using TUnit.Assertions.AssertConditions.Combiners;

namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected> : BaseAssertCondition<TActual, TExpected>
{
    internal TExpected? ExpectedValue { get; }
    
    internal AssertCondition(TExpected? expected)
    {
        ExpectedValue = expected;
    }
    
    public bool Assert(TActual actualValue)
    {
        ActualValue = actualValue;
        return Passes(actualValue);
    }
    
    private Func<TActual, string>? MessageFactory { get; set; }

    public string Message => MessageFactory?.Invoke(ActualValue) ?? DefaultMessage;
    
    public AssertCondition<TActual, TExpected> WithMessage(Func<TActual, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}