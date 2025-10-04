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
    private static bool? _isFSharpSupported;

    [MethodImpl(MethodImplOptions.AggressiveInlining
#if NET
                | MethodImplOptions.AggressiveOptimization
#endif
    )]
    public static ValueTask Convert(Action action)
    {
        action();
        return default(ValueTask);
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
    public static async ValueTask Convert(Func<Task> action)
    {
        var task = action();

        if (task is { IsCompleted: true, IsFaulted: false })
        {
            return;
        }

        await task;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining
#if NET
                | MethodImplOptions.AggressiveOptimization
#endif
    )]
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("ConvertObject uses reflection to handle custom awaitable types and F# async. For AOT compatibility, use Task or ValueTask directly.")]
    [RequiresDynamicCode("ConvertObject may require dynamic invocation for custom awaitable types. For AOT compatibility, use Task or ValueTask directly.")]
    #endif
    public static async ValueTask ConvertObject(object? invoke)
    {
        if (invoke is Delegate @delegate)
        {
            invoke = @delegate.DynamicInvoke();
        }

        if (invoke is null)
        {
            return;
        }

        if (invoke is Task task)
        {
            if (task is { IsCompleted: true, IsFaulted: false })
            {
                return;
            }

            await task;
            return;
        }

        if (invoke is ValueTask valueTask)
        {
            if(valueTask is { IsCompleted: true, IsFaulted: false })
            {
                return;
            }

            await valueTask;
            return;
        }

        // If it has a GetAwaiter method, we can assume it's an awaitable type
        if (TryGetAwaitableTask(invoke, out var awaitable))
        {
            if (awaitable is { IsCompleted: true, IsFaulted: false })
            {
                return;
            }

            await awaitable;
            return;
        }

        var type = invoke.GetType();
        if (type.IsGenericType
            && type.GetGenericTypeDefinition().FullName == "Microsoft.FSharp.Control.FSharpAsync`1")
        {
            // F# async support requires reflection and is not AOT-compatible
            // Users should use Task-based APIs for AOT scenarios
            await StartAsFSharpTaskSafely(invoke, type);
            return;
        }
    }

    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods, "Microsoft.FSharp.Control.FSharpAsync", "FSharp.Core")]
    [System.Diagnostics.CodeAnalysis.DynamicDependency("StartAsTask", "Microsoft.FSharp.Control.FSharpAsync", "FSharp.Core")]
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("F# async support requires FSharp.Core types and reflection. For AOT, use Task-based APIs.")]
    [RequiresDynamicCode("F# async interop requires MakeGenericMethod. For AOT, use Task-based APIs.")]
    #endif
    private static ValueTask StartAsFSharpTask(object invoke, Type type)
    {
        var startAsTaskOpenGenericMethod = (_fSharpAsyncType ??= type.Assembly.GetType("Microsoft.FSharp.Control.FSharpAsync"))!
            .GetRuntimeMethods()
            .First(m => m.Name == "StartAsTask");

        var fSharpTask = (Task) startAsTaskOpenGenericMethod.MakeGenericMethod(type.GetGenericArguments()[0])
            .Invoke(null, [invoke, null, null])!;

        // Ensure exceptions are observed by accessing the Exception property
        if (fSharpTask.IsFaulted)
        {
            _ = fSharpTask.Exception;
        }

        return new ValueTask(fSharpTask);
    }

    /// <summary>
    /// Safely invokes F# async conversion with proper suppression for the call site.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("F# async is an optional feature. AOT applications should use Task-based APIs.")]
    [RequiresDynamicCode("F# async requires runtime code generation. This is documented as not AOT-compatible.")]
    #endif
    private static async ValueTask StartAsFSharpTaskSafely(object invoke, Type type)
    {
        if (IsFSharpAsyncSupported())
        {
            await StartAsFSharpTask(invoke, type);
            return;
        }
    }

    /// <summary>
    /// Checks if F# async support is available (not in AOT mode).
    /// </summary>
    private static bool IsFSharpAsyncSupported()
    {
        if (_isFSharpSupported.HasValue)
        {
            return _isFSharpSupported.Value;
        }

        // In AOT mode, we can't use reflection to invoke F# async methods
        // This is a runtime check that will return false in AOT scenarios
        try
        {
            var fsharpCoreAssembly = Type.GetType("Microsoft.FSharp.Control.FSharpAsync, FSharp.Core")?.Assembly;
            _isFSharpSupported = fsharpCoreAssembly != null;
            return _isFSharpSupported.Value;
        }
        catch
        {
            _isFSharpSupported = false;
            return false;
        }
    }

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("GetAwaiter pattern detection requires reflection for custom awaitable types. For AOT, use Task/ValueTask.")]
    [RequiresDynamicCode("Custom awaitable handling may require dynamic invocation. For AOT, use Task/ValueTask.")]
    #endif
    public static bool TryGetAwaitableTask(object awaitable, [NotNullWhen(true)] out Task? task)
    {
        var getAwaiter = awaitable.GetType().GetMethod("GetAwaiter", Type.EmptyTypes);

        if (getAwaiter == null)
        {
            task = null;
            return false;
        }

        var awaiter = getAwaiter.Invoke(awaitable, null);

        if (awaiter == null)
        {
            task = null;
            return false;
        }

        var isCompletedProp = awaiter.GetType().GetProperty("IsCompleted");

        if (isCompletedProp == null)
        {
            task = null;
            return false;
        }

        var isCompleted = isCompletedProp.GetValue(awaiter) as bool?;

        if (isCompleted == true)
        {
            task = Task.CompletedTask;
            return true;
        }

        if (!isCompleted.HasValue)
        {
            task = null;
            return false;
        }

        var tcs = new TaskCompletionSource<object?>();

        var onCompleted = awaiter.GetType().GetMethod("OnCompleted", [typeof(Action)]);

        onCompleted!.Invoke(awaiter, [new Action(() => tcs.SetResult(null))]);

        task = tcs.Task;

        return true;
    }
}
