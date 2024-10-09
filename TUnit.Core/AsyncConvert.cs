using System.Diagnostics;

namespace TUnit.Core;

public static class AsyncConvert
{
    [DebuggerHidden]
    public static Task Convert(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    [DebuggerHidden]
    public static async Task Convert(Func<Task> action)
    {
        await action();
    }
    
    [DebuggerHidden]
    public static async Task Convert(Func<ValueTask> action)
    {
        await action();
    }
}