namespace TUnit.Assertions.AssertConditions.Throws;

public abstract class DelegateAssertCondition<T>
{
    internal readonly IReadOnlyCollection<DelegateAssertCondition<T>> NestedAssertConditions;

    internal DelegateAssertCondition(IReadOnlyCollection<DelegateAssertCondition<T>> nestedAssertConditions)
    {
        NestedAssertConditions = [..nestedAssertConditions, this];
    }

    private Func<T?, Exception?, string>? MessageFactory { get; set; }

    protected Exception? Exception { get; private set; }
    protected T? ActualValue { get; private set; } = default!;

    public bool Assert(DelegateInvocationResult<T> delegateInvocationResult)
    {
        ActualValue = delegateInvocationResult.Result;
        Exception = delegateInvocationResult.Exception;
        
        return Passes(ActualValue, Exception);
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
    internal readonly IReadOnlyCollection<DelegateAssertCondition> NestedAssertConditions;

    internal DelegateAssertCondition(IReadOnlyCollection<DelegateAssertCondition> nestedAssertConditions)
    {
        NestedAssertConditions = [..nestedAssertConditions, this];
    }

    private Func<Exception?, string>? MessageFactory { get; set; }

    protected Exception? Exception { get; private set; }

    public bool Assert(Exception? exception)
    {
        Exception = exception;
        
        return Passes(Exception);
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