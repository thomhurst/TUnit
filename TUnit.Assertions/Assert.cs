using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public static class Assert
{
    public static void That<T>(T value, AssertCondition<T, T> assertCondition)
    {
        if (!assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static void That(Action value, DelegateAssertCondition assertCondition)
    {
        var exception = value.InvokeAndGetException();
        
        if (!assertCondition.Assert(exception))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static void That<T>(Func<T> value, DelegateAssertCondition<T> assertCondition)
    {
        var delegateInvocationResult = value.InvokeAndGetException();
        
        if (!assertCondition.Assert(delegateInvocationResult))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static async Task That(Func<Task> value, DelegateAssertCondition assertCondition)
    {
        var exception = await value.InvokeAndGetExceptionAsync();
        
        if (!assertCondition.Assert(exception))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static async Task That<T>(Func<Task<T>> value, DelegateAssertCondition<T> assertCondition)
    {
        var invocationResult = await value.InvokeAndGetExceptionAsync();
        
        if (!assertCondition.Assert(invocationResult))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
}