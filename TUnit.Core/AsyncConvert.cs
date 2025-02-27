using System.Diagnostics;

namespace TUnit.Core;

[StackTraceHidden]
public static class AsyncConvert
{
    public static Task Convert(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    public static async Task Convert(Func<Task> action)
    {
        await action();
    }
    
    public static async Task Convert(Func<ValueTask> action)
    {
        await action();
    }

    public static async Task ConvertObject(object? invoke)
    {
        if (invoke is Func<object> syncFunc)
        {
            syncFunc();
        }
        
        if (invoke is Func<Task> asyncFunc)
        {
            await asyncFunc();
        }
        
        if (invoke is Func<ValueTask> asyncValueFunc)
        {
            await asyncValueFunc();
        }
        
        if (invoke is Task task)
        {
            await task;
        }

        if (invoke is ValueTask valueTask)
        {
            await valueTask;
        }
    }
}