using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Provides methods to convert tasks to async methods.
/// </summary>
public static class AsyncConvert
{
    [MethodImpl(MethodImplOptions.AggressiveInlining
#if NET
                | MethodImplOptions.AggressiveOptimization
#endif
    )]
    public static ValueTask Convert(Action action)
    {
        action();
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining
#if NET
                | MethodImplOptions.AggressiveOptimization
#endif
    )]
    public static ValueTask Convert(Func<ValueTask> action)
    {
        return action();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining
#if NET
                | MethodImplOptions.AggressiveOptimization
#endif
    )]
    public static ValueTask Convert(Func<Task> action)
    {
        var task = action();

        if (task.IsCompleted && !task.IsFaulted)
        {
            return default;
        }

        return new ValueTask(task);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining
#if NET
                | MethodImplOptions.AggressiveOptimization
#endif
    )]
    public static ValueTask ConvertObject(object? invoke)
    {
        if (invoke is null)
        {
            return default;
        }
        
        if (invoke is Func<object> syncFunc)
        {
            syncFunc();
            return default;
        }
        
        if (invoke is Func<Task> asyncFunc)
        {
            return Convert(asyncFunc);
        }
        
        if (invoke is Func<ValueTask> asyncValueFunc)
        {
            return Convert(asyncValueFunc);
        }
        
        
        if (invoke is Task task)
        {
            if(task is { IsCompleted: true, IsFaulted: false })
            {
                return default;
            }
            
            if(task.IsFaulted || !task.IsCompleted)
            {
                return new ValueTask(task);
            }
        }

        if (invoke is ValueTask valueTask)
        {
            return valueTask;
        }

        throw new ArgumentException("Invalid object type");
    }
}