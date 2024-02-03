using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition<TActual>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    internal BaseAssertCondition(AssertionBuilder<TActual> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
        
        And = new And<TActual>(this);
        Or = new Or<TActual>(this);
    }
    
    public TaskAwaiter GetAwaiter()
    {
        return AssertAsync().GetAwaiter();
    }

    private async Task AssertAsync()
    {
        var assertionData = await AssertionBuilder.GetAssertionData();
        
        AssertAndThrow(assertionData.Result, assertionData.Exception);
    }

    protected TActual? ActualValue { get; private set; }
    protected Exception? Exception { get; private set; }


    protected internal virtual string Message => MessageFactory?.Invoke(ActualValue, Exception) ?? DefaultMessage;

    private Func<TActual?, Exception?, string>? MessageFactory { get; set; }
    
    public BaseAssertCondition<TActual> WithMessage(Func<TActual?, Exception?, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
    
    protected abstract string DefaultMessage { get; }

    private void AssertAndThrow(TActual? actual, Exception? exception)
    {
        if (!Assert(actual, exception))
        {
            throw new AssertionException(Message);
        }
    }
    
    internal bool Assert(TActual? actualValue, Exception? exception)
    {
        ActualValue = actualValue;
        Exception = exception;
        return IsInverted ? !Passes(actualValue, exception) : Passes(actualValue, exception);
    }

    protected internal abstract bool Passes(TActual? actualValue, Exception? exception);

    public And<TActual> And { get; }
    public Or<TActual> Or { get; }

    internal BaseAssertCondition<TActual> Invert(Func<TActual?, Exception?, string> messageFactory)
    {
        WithMessage(messageFactory);
        IsInverted = true;
        return this;
    }
    
    protected bool IsInverted { get; set; }
}