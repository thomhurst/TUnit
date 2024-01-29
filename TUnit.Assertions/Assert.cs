using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public static class Assert
{
    public static void That<T>(T value, AssertCondition<T> assertCondition)
    {
        if (!assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static void That(Action value, DelegateAssertCondition assertCondition)
    {
        if (!assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static void That<T>(Func<T> value, DelegateAssertCondition<T> assertCondition)
    {
        if (!assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static async Task That(Func<Task> value, AssertCondition assertCondition)
    {
        if (!await assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static async Task That<T>(Func<Task<T>> value, DelegateAssertCondition<T> assertCondition)
    {
        var invocationResult = await value.InvokeAndGetExceptionAsync();
        if (!assertCondition.Assert(invocationResult.Result, invocationResult.Exception))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
}