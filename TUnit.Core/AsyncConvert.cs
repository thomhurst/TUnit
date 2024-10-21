﻿using System.Diagnostics;

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

    public static Task Convert(object? invoke)
    {
        if (invoke is Task task)
        {
            return task;
        }

        if (invoke is ValueTask valueTask)
        {
            return valueTask.AsTask();
        }
        
        return Task.CompletedTask;
    }
}