namespace TUnit.Assertions;

public abstract class AssertCondition<T> : IAssertCondition<T>
{
    internal AssertCondition(T expected)
    {
        ExpectedValue = expected;
    }
    
    internal abstract Func<(T ExpectedValue, T ActualValue), string> MessageFactory { get; set; }

    internal T ExpectedValue { get; }
    private T ActualValue { get; set; } = default!;

    public bool Assert(T actualValue)
    {
        ActualValue = actualValue;
        return Passes(actualValue);
    }
    
    protected abstract bool Passes(T actualValue);

    public string Message => MessageFactory.Invoke((ExpectedValue, ActualValue));
    
    public IAssertCondition<T> WithMessage(Func<(T ExpectedValue, T ActualValue), string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}