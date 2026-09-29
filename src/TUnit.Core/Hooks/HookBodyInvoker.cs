namespace TUnit.Core.Hooks;

/// <summary>
/// Invokes a hook <c>Body</c> delegate with async-method semantics.
/// </summary>
/// <remarks>
/// Source-generated hook bodies call the hook method directly, without an async state machine of their
/// own. Invoking them through this async wrapper keeps the behaviour of an async body:
/// <list type="bullet">
/// <item>A synchronous exception becomes a faulted task, and an <see cref="OperationCanceledException"/>
/// becomes a canceled task, instead of throwing out of the executor's <c>action()</c> call.</item>
/// <item>ExecutionContext changes (AsyncLocal values, culture) made synchronously by the hook do not leak
/// into the caller. Hooks opt in to flowing them with <see cref="Context.AddAsyncLocalValues"/>.</item>
/// </list>
/// The wrapper completes without allocating when the hook completes synchronously.
/// </remarks>
internal static class HookBodyInvoker
{
    public static async ValueTask InvokeAsync<T>(Func<T, CancellationToken, ValueTask> body, T context, CancellationToken cancellationToken)
    {
        await body(context, cancellationToken);
    }

    public static async ValueTask InvokeAsync(Func<object, TestContext, CancellationToken, ValueTask> body, object instance, TestContext context, CancellationToken cancellationToken)
    {
        await body(instance, context, cancellationToken);
    }
}
