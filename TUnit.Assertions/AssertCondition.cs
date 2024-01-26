namespace TUnit.Assertions;

public abstract class AssertCondition<T> : IAssertCondition<T>
{
    internal AssertCondition()
    {
    }

    private Func<T, string>? MessageFactory { get; set; }

    protected T ActualValue { get; private set; } = default!;

    public bool Assert(T actualValue)
    {
        ActualValue = actualValue;
        return Passes(actualValue);
    }

    public abstract string DefaultMessage { get; }

    protected abstract bool Passes(T actualValue);

    public string Message => MessageFactory?.Invoke(ActualValue) ?? DefaultMessage;
    
    public IAssertCondition<T> WithMessage(Func<T, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}