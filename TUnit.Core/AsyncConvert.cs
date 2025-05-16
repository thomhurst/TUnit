using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Provides methods to convert tasks to async methods.
/// </summary>
public static class AsyncConvert
{
    private static Type? _fSharpAsyncType;
    
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

        var type = invoke.GetType();
        if (type.IsGenericType 
            && type.GetGenericTypeDefinition().FullName == "Microsoft.FSharp.Control.FSharpAsync`1")
        {
        #pragma warning disable
            return StartAsFSharpTask(invoke, type);
        #pragma warning restore
        }

        throw new ArgumentException("Invalid object type: " + type.Name, nameof(invoke));
    }
    
    [RequiresDynamicCode("Dynamic code is required to call F# async methods.")]
    [RequiresUnreferencedCode("Dynamic code is required to call F# async methods.")]
    private static ValueTask StartAsFSharpTask(object invoke, Type type)
    {
        var startAsTaskOpenGenericMethod = (_fSharpAsyncType ??= type.Assembly.GetType("Microsoft.FSharp.Control.FSharpAsync"))!
            .GetRuntimeMethods()
            .First(m => m.Name == "StartAsTask");

        var fSharpTask = (Task)startAsTaskOpenGenericMethod.MakeGenericMethod(type.GetGenericArguments()[0])
            .Invoke(null, [invoke, null, null])!;
            
        return new ValueTask(fSharpTask);
    }
}