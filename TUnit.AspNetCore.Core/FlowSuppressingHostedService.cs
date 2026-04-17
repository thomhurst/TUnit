using Microsoft.Extensions.Hosting;

namespace TUnit.AspNetCore;

/// <summary>
/// Wraps an <see cref="IHostedService"/> so its <see cref="IHostedService.StartAsync"/>
/// runs on a thread-pool worker with a clean <see cref="ExecutionContext"/>.
/// Background tasks spawned anywhere inside <c>StartAsync</c> — synchronously or
/// after an <c>await</c> — inherit that clean context, so activities they later
/// emit do not inherit the test's ambient <see cref="System.Diagnostics.Activity.Current"/>
/// as their parent.
/// </summary>
/// <remarks>
/// Implements <see cref="IHostedLifecycleService"/> so the Host's lifecycle hooks keep
/// firing for inner services that implement it — the Host uses an <c>is</c> check
/// against the registered instance, so without passthrough wrapping would silently
/// drop those hooks.
/// </remarks>
internal sealed class FlowSuppressingHostedService(IHostedService inner) : IHostedLifecycleService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        RunOnCleanContext(inner.StartAsync, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) =>
        inner.StopAsync(cancellationToken);

    public Task StartingAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? RunOnCleanContext(lifecycle.StartingAsync, cancellationToken)
            : Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? RunOnCleanContext(lifecycle.StartedAsync, cancellationToken)
            : Task.CompletedTask;

    // Stop lifecycle is intentionally not wrapped: stop methods typically signal
    // cancellation and await shutdown rather than spawning new long-running background
    // work, so context capture during Stop is not the span-leak vector that Start is.
    public Task StoppingAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? lifecycle.StoppingAsync(cancellationToken)
            : Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? lifecycle.StoppedAsync(cancellationToken)
            : Task.CompletedTask;

    // Dispatch onto a thread-pool worker with a clean captured ExecutionContext by
    // combining SuppressFlow + Task.Run. Unlike wrapping `using (SuppressFlow()) return op(ct);`
    // which only suppresses during the synchronous body, this keeps the inner operation
    // running under a clean context through awaits — every `Task.Run` inside `op` also
    // captures clean context.
    private static Task RunOnCleanContext(Func<CancellationToken, Task> op, CancellationToken ct)
    {
        using var _ = ExecutionContext.SuppressFlow();
        return Task.Run(() => op(ct), ct);
    }
}
