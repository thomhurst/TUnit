using System.Reflection;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Bridges <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> into <see cref="Task"/>,
/// avoiding the state-machine overhead of <see cref="ValueTask.AsTask"/> when the
/// <see cref="ValueTask"/> already completed synchronously. Invoked from dynamic test Expression
/// trees built in <c>TestRegistry</c> so that the common sync-completion path returns
/// <see cref="Task.CompletedTask"/> (non-generic) or <c>Task.FromResult</c> (generic) directly.
/// Note: <see cref="Task.FromResult{TResult}"/> only caches <c>bool</c>/<c>null</c>; other types
/// still allocate a fresh <see cref="Task{TResult}"/>, but that cost is smaller than the
/// <see cref="ValueTask.AsTask"/> state-machine path.
/// </summary>
internal static class ValueTaskBridge
{
    public static Task ToTask(ValueTask valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            return Task.CompletedTask;
        }

        return valueTask.AsTask();
    }

    public static Task<T> ToTask<T>(ValueTask<T> valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            return Task.FromResult(valueTask.Result);
        }

        return valueTask.AsTask();
    }

    /// <summary>Pre-resolved <see cref="MethodInfo"/> for the non-generic <see cref="ToTask(ValueTask)"/>.</summary>
    internal static readonly MethodInfo ToTaskNonGenericMethod =
        typeof(ValueTaskBridge).GetMethod(nameof(ToTask), [typeof(ValueTask)])!;

    /// <summary>
    /// Pre-resolved generic method definition for <see cref="ToTask{T}(ValueTask{T})"/>.
    /// Callers invoke <see cref="MethodInfo.MakeGenericMethod"/> on this instance per element type.
    /// </summary>
    internal static readonly MethodInfo ToTaskGenericMethodDefinition =
        typeof(ValueTaskBridge).GetMethods()
            .First(m => m.Name == nameof(ToTask) && m.IsGenericMethodDefinition);
}
