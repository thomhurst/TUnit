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
    
    public static async Task ThatAsync<T>(T value, AsyncAssertCondition<T> assertCondition)
    {
        if (!await assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static void That(Action value, SynchronousDelegateAssertCondition assertCondition)
    {
        if (!assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
    
    public static async Task ThatAsync(Func<Task> value, AsynchronousDelegateAssertCondition assertCondition)
    {
        if (!await assertCondition.Assert(value))
        {
            throw new AssertionException(assertCondition.Message);
        }
    }
}