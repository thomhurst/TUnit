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

    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods, "Microsoft.FSharp.Control.FSharpAsync", "FSharp.Core")]
    [System.Diagnostics.CodeAnalysis.DynamicDependency("StartAsTask", "Microsoft.FSharp.Control.FSharpAsync", "FSharp.Core")]
    [UnconditionalSuppressMessage("Trimming", "IL2077:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The source field does not have matching annotations.",
        Justification = "F# async support requires FSharp.Core types. The DynamicDependency attributes preserve required members.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "F# async type resolution is preserved through DynamicDependency. For AOT, ensure FSharp.Core is properly referenced.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.",
        Justification = "F# async interop requires MakeGenericMethod. F# tests in AOT scenarios should use Task-based APIs.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060:Call to \'System.Reflection.MethodInfo.MakeGenericMethod\' can not be statically analyzed. It\'s not possible to guarantee the availability of requirements of the generic method.",
        Justification = "Generic method instantiation for F# async types. The type parameter comes from the F# async type itself.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "F# async support is optional. Tests using F# async should ensure FSharp.Core is preserved in AOT scenarios.")]
    [RequiresUnreferencedCode("F# async support requires dynamic access to FSharp.Core types.")]
    [RequiresDynamicCode("F# async support requires runtime code generation for generic method instantiation.")]
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
    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member with RequiresUnreferencedCodeAttribute",
        Justification = "F# async is an optional feature. AOT applications should use Task-based APIs.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Using member with RequiresDynamicCodeAttribute",
        Justification = "F# async requires runtime code generation. This is documented as not AOT-compatible.")]
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

    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "GetAwaiter pattern detection for custom awaitables. For AOT, use standard Task/ValueTask types or implement IAsyncEnumerable.")]
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
