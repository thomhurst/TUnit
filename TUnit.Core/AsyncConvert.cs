using System.Diagnostics;

namespace TUnit.Core;

public static class AsyncConvert
{
    [StackTraceHidden]
    public static Task Convert(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    [StackTraceHidden]
    public static async Task Convert(Func<Task> action)
    {
        await action();
    }
    
    [StackTraceHidden]
    public static async Task Convert(Func<ValueTask> action)
    {
        await action();
    }
}