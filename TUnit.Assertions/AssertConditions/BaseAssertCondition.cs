using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition<TActual>
{
    internal readonly AssertionBuilder<TActual> AssertionBuilder;

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
    
    
    protected internal abstract string Message { get; }
    
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
        return Passes(actualValue, exception);
    }

    protected internal abstract bool Passes(TActual? actualValue, Exception? exception);

    public And<TActual> And { get; }
    public Or<TActual> Or { get; }
}