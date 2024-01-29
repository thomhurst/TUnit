using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Combiners;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public static class Assert
{
    public static void That<TActual, TExpected>(TActual value, AssertCondition<TActual, TExpected> assertCondition)
    {
        if (!assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static void That<TActual, TExpected>(TActual value, AssertConditionOr<TActual, TExpected> orCondition)
    {
        if (!orCondition.Assert(value))
        {
            throw new AssertionException(orCondition.Message);
        }
    }
    
    public static void That<TActual, TExpected>(TActual value, AssertConditionAnd<TActual, TExpected> andCondition)
    {
        if (!andCondition.Assert(value))
        {
            throw new AssertionException(andCondition.Message);
        }
    }
    
    public static Exception? That(Action value, DelegateAssertCondition assertCondition)
    {
        var exception = value.InvokeAndGetException();

        if (!assertCondition.Assert(exception))
        {
            throw new AssertionException(assertCondition.Message);
        }

        return exception;
    }
    
    public static DelegateInvocationResult<T> That<T>(Func<T> value, DelegateAssertCondition<T> assertCondition)
    {
        var delegateInvocationResult = value.InvokeAndGetException();

        if (!assertCondition.Assert(delegateInvocationResult))
        {
            throw new AssertionException(assertCondition.Message);
        }

        return delegateInvocationResult;
    }
    
    public static async Task<Exception?> That(Func<Task> value, DelegateAssertCondition assertCondition)
    {
        var exception = await value.InvokeAndGetExceptionAsync();

        if (!assertCondition.Assert(exception))
        {
            throw new AssertionException(assertCondition.Message);
        }

        return exception;
    }
    
    public static async Task<DelegateInvocationResult<T>> That<T>(Func<Task<T>> value, DelegateAssertCondition<T> assertCondition)
    {
        var delegateInvocationResult = await value.InvokeAndGetExceptionAsync();

        if (!assertCondition.Assert(delegateInvocationResult))
        {
            throw new AssertionException(assertCondition.Message);
        }

        return delegateInvocationResult;
    }
}