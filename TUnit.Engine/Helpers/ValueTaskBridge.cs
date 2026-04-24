using System.Diagnostics.CodeAnalysis;
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
    /// On .NET 8+ uses the typed <see cref="Type.GetMethod(string, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)"/>
    /// overload so a future <c>ToTask</c> overload cannot silently match; on netstandard2.0
    /// (which lacks that overload and <see cref="Type.MakeGenericMethodParameter(int)"/>)
    /// falls back to a name-scan that uses <see cref="Enumerable.Single{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// so an accidental duplicate match throws instead of silently picking one.
    /// </summary>
    internal static readonly MethodInfo ToTaskGenericMethodDefinition = ResolveToTaskGenericMethodDefinition();

#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "MakeGenericType is called on ValueTask<> with a generic method parameter placeholder at startup to find the MethodInfo for ToTask<T>; the shared generic-type vtable is always available, and the resolved MethodInfo is then instantiated per-element-type in the same expression tree paths that already require dynamic code (AOT paths use direct delegates).")]
#endif
    private static MethodInfo ResolveToTaskGenericMethodDefinition()
    {
#if NET8_0_OR_GREATER
        return typeof(ValueTaskBridge).GetMethod(
            name: nameof(ToTask),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(ValueTask<>).MakeGenericType(Type.MakeGenericMethodParameter(0))],
            modifiers: null)!;
#else
        return typeof(ValueTaskBridge).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(ToTask) && m.IsGenericMethodDefinition);
#endif
    }
}
