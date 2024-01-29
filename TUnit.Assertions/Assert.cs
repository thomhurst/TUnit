using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public static class Assert
{
    public static void That<T>(T value, AssertCondition<T, T> assertCondition)
    {
        foreach (var condition in assertCondition.NestedAssertConditions)
        {
            if (!condition.Assert(value))
            {
                throw new AssertionException(condition.Message);
            }
        }
    }
    
    public static Exception? That(Action value, DelegateAssertCondition assertCondition)
    {
        var exception = value.InvokeAndGetException();

        foreach (var condition in assertCondition.NestedAssertConditions)
        {
            if (!condition.Assert(exception))
            {
                throw new AssertionException(condition.Message);
            }
        }

        return exception;
    }
    
    public static DelegateInvocationResult<T> That<T>(Func<T> value, DelegateAssertCondition<T> assertCondition)
    {
        var delegateInvocationResult = value.InvokeAndGetException();

        foreach (var condition in assertCondition.NestedAssertConditions)
        {
            if (!condition.Assert(delegateInvocationResult))
            {
                throw new AssertionException(condition.Message);
            }
        }

        return delegateInvocationResult;
    }
    
    public static async Task<Exception?> That(Func<Task> value, DelegateAssertCondition assertCondition)
    {
        var exception = await value.InvokeAndGetExceptionAsync();

        foreach (var condition in assertCondition.NestedAssertConditions)
        {
            if (!condition.Assert(exception))
            {
                throw new AssertionException(condition.Message);
            }
        }

        return exception;
    }
    
    public static async Task<DelegateInvocationResult<T>> That<T>(Func<Task<T>> value, DelegateAssertCondition<T> assertCondition)
    {
        var invocationResult = await value.InvokeAndGetExceptionAsync();

        foreach (var condition in assertCondition.NestedAssertConditions)
        {
            if (!condition.Assert(invocationResult))
            {
                throw new AssertionException(condition.Message);
            }
        }

        return invocationResult;
    }
}