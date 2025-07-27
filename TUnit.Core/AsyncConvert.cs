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
        if (invoke is null)
        {
            return;
        }

        if (invoke is Func<object> syncFunc)
        {
            syncFunc();
            return;
        }

        if (invoke is Func<Task> asyncFunc)
        {
            await Convert(asyncFunc);
            return;
        }

        if (invoke is Func<ValueTask> asyncValueFunc)
        {
            await Convert(asyncValueFunc);
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
            await StartAsFSharpTask(invoke, type);
            return;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2077:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The source field does not have matching annotations.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060:Call to \'System.Reflection.MethodInfo.MakeGenericMethod\' can not be statically analyzed. It\'s not possible to guarantee the availability of requirements of the generic method.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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
