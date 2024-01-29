using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Conditions;
using TUnit.Assertions.Exceptions;

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
    
    public static async Task That(Func<Task> value, AsyncAssertCondition assertCondition)
    {
        if (!await assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static async Task That<T>(Func<Task<T>> value, AsyncAssertCondition<T> assertCondition)
    {
        if (!await assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
}