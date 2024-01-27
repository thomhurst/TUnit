namespace TUnit.Assertions.AssertConditions.Conditions;

public abstract class DelegateAssertCondition<T>
{
    internal DelegateAssertCondition()
    {
    }

    private Func<T?, Exception?, string>? MessageFactory { get; set; }

    protected Exception? Exception { get; private set; }
    protected T? ActualValue { get; private set; } = default!;

    public virtual bool Assert(T? actualValue, Exception? exception)
    {
        Exception = exception;
        ActualValue = actualValue;
        return Passes(actualValue, exception);
    }

    public abstract string DefaultMessage { get; }

    protected abstract bool Passes(T? actualValue, Exception? exception);

    public string Message => MessageFactory?.Invoke(ActualValue, Exception) ?? DefaultMessage;
    
    public DelegateAssertCondition<T> WithMessage(Func<T?, Exception?, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}

public abstract class DelegateAssertCondition
{
    internal DelegateAssertCondition()
    {
    }

    private Func<Exception?, string>? MessageFactory { get; set; }

    protected Exception? Exception { get; private set; }

    public virtual bool Assert(Exception? exception)
    {
        Exception = exception;
        return Passes(exception);
    }

    public abstract string DefaultMessage { get; }

    protected abstract bool Passes(Exception? exception);

    public string Message => MessageFactory?.Invoke(Exception) ?? DefaultMessage;
    
    public DelegateAssertCondition WithMessage(Func<Exception?, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}