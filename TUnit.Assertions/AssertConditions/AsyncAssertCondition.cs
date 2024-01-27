namespace TUnit.Assertions;

public abstract class AsyncAssertCondition<T>
{
    internal AsyncAssertCondition()
    {
    }

    private Func<T, string>? MessageFactory { get; set; }

    protected T ActualValue { get; private set; } = default!;

    public async Task<bool> Assert(T actualValue)
    {
        ActualValue = actualValue;
        return await Passes(actualValue);
    }

    public abstract string DefaultMessage { get; }

    protected abstract Task<bool> Passes(T actualValue);

    public string Message => MessageFactory?.Invoke(ActualValue) ?? DefaultMessage;
    
    public AsyncAssertCondition<T> WithMessage(Func<T, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}