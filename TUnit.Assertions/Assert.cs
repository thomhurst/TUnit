using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Conditions;
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
    
    public static async Task ThatAsync<T>(T value, AsyncAssertCondition<T> assertCondition)
    {
        if (!await assertCondition.Assert(value))
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
        var result = value.InvokeAndGetException();
        
        if (!assertCondition.Assert(result.Item1, result.Item2))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static async Task ThatAsync(Func<Task> value, DelegateAssertCondition assertCondition)
    {
        var exception = await value.InvokeAndGetExceptionAsync();
        
        if (!assertCondition.Assert(exception))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static async Task ThatAsync<T>(Func<Task<T>> value, DelegateAssertCondition<T> assertCondition)
    {
        var result = await value.InvokeAndGetExceptionAsync();
        
        if (!assertCondition.Assert(result.Item1, result.Item2))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
}