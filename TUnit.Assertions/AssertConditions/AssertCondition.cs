namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<T>
{
    internal AssertCondition()
    {
    }

    private Func<T, string>? MessageFactory { get; set; }

    protected T ActualValue { get; private set; } = default!;

    public virtual bool Assert(T actualValue)
    {
        ActualValue = actualValue;
        return Passes(actualValue);
    }

    public abstract string DefaultMessage { get; }

    protected abstract bool Passes(T actualValue);

    public string Message => MessageFactory?.Invoke(ActualValue) ?? DefaultMessage;
    
    public AssertCondition<T> WithMessage(Func<T, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}