namespace TUnit.Assertions.AssertConditions;

public abstract class AsyncAssertCondition
{
    internal AsyncAssertCondition()
    {
        And = new AsyncAnd(this);
        Or = new AsyncOr(this);
    }

    private Func<Exception?, string>? MessageFactory { get; set; }

    protected Exception? Exception { get; private set; }

    public async Task<bool> Assert(Func<Task> @delegate)
    {
        try
        {
            await @delegate();
        }
        catch (Exception e)
        {
            Exception = e;
        }
        
        return Passes(Exception);
    }

    internal void SetActual(Exception? exception)
    {
        Exception = exception;
    }

    public abstract string DefaultMessage { get; }

    protected abstract bool Passes(Exception? exception);

    public string Message => MessageFactory?.Invoke(Exception) ?? DefaultMessage;
    
    public AsyncAssertCondition WithMessage(Func<Exception?, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
    
    public AsyncAnd And { get; }
    public AsyncOr Or { get; }
}

public abstract class AsyncAssertCondition<T>
{
    internal AsyncAssertCondition()
    {
        And = new AsyncAnd<T>(this);
        Or = new AsyncOr<T>(this);
    }

    private Func<T?, Exception?, string>? MessageFactory { get; set; }

    protected T? ActualValue { get; private set; }
    protected Exception? Exception { get; private set; }

    public async Task<bool> Assert(Func<Task<T>> @delegate)
    {
        try
        {
            ActualValue = await @delegate();
        }
        catch (Exception e)
        {
            Exception = e;
        }
        
        return Passes(ActualValue, Exception);
    }

    internal void SetActual(T? actualValue, Exception? exception)
    {
        ActualValue = actualValue;
        Exception = exception;
    }

    public abstract string DefaultMessage { get; }

    protected abstract bool Passes(T? actualValue, Exception? exception);

    public string Message => MessageFactory?.Invoke(ActualValue, Exception) ?? DefaultMessage;
    
    public AsyncAssertCondition<T> WithMessage(Func<T?, Exception?, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
    
    public AsyncAnd<T> And { get; }
    public AsyncOr<T> Or { get; }
}