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

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Microsoft.FSharp.Control.FSharpAsync", "FSharp.Core")]
    [DynamicDependency("StartAsTask", "Microsoft.FSharp.Control.FSharpAsync", "FSharp.Core")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("Trimming", "IL2060:Call to \'System.Reflection.MethodInfo.MakeGenericMethod\' can not be statically analyzed. It\'s not possible to guarantee the availability of requirements of the generic method.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    [UnconditionalSuppressMessage("Trimming", "IL2077:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The source field does not have matching annotations.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
    private static ValueTask StartAsFSharpTask(object invoke, Type type)
    {
        var startAsTaskOpenGenericMethod = (_fSharpAsyncType ??= type.Assembly.GetType("Microsoft.FSharp.Control.FSharpAsync"))!
            .GetRuntimeMethods()
            .FirstOrDefault(m => m.Name == "StartAsTask");

        if (startAsTaskOpenGenericMethod is null)
        {
            return default;
        }

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
    private static async ValueTask StartAsFSharpTaskSafely(object invoke, Type type)
    {
        if (IsFSharpAsyncSupported())
        {
            await StartAsFSharpTask(invoke, type);
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

    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
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
