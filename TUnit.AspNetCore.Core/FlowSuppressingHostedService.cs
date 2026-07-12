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
/// <para>
/// Implements <see cref="IHostedLifecycleService"/> so the Host's lifecycle hooks keep
/// firing for inner services that implement it — the Host uses an <c>is</c> check
/// against the registered instance, so without passthrough wrapping would silently
/// drop those hooks.
/// </para>
/// <para>
/// Also implements <see cref="IAsyncDisposable"/> and <see cref="IDisposable"/> so the
/// DI container forwards disposal to the inner service when the host is disposed.
/// Without this, wrapped services that own unmanaged resources leak silently because
/// the container only sees the non-disposable wrapper.
/// </para>
/// <para>
/// The stop-phase methods are additionally guarded to run the inner service's stop
/// exactly once. Minimal-hosting SUTs park their own <c>app.Run()</c> in
/// <c>WaitForShutdownAsync</c>, which calls <c>Host.StopAsync</c> when
/// <c>ApplicationStopping</c> fires — concurrently with the <c>Host.StopAsync</c> that
/// <c>WebApplicationFactory.DisposeAsync</c> already has in flight (the trigger of that
/// very <c>ApplicationStopping</c>). <c>Host.StopAsync</c> has no concurrency guard, so
/// every hosted service's <c>StopAsync</c> runs twice in parallel, breaking services
/// with non-thread-safe shutdown (e.g. Rebus' bus dispose throws
/// <see cref="ObjectDisposedException"/>). See https://github.com/thomhurst/TUnit/issues/6339.
/// All concurrent and repeated callers observe the single underlying stop task.
/// </para>
/// </remarks>
internal sealed class FlowSuppressingHostedService(IHostedService inner) : IHostedLifecycleService, IAsyncDisposable, IDisposable
{
    private readonly object _stopGate = new();
    private Task? _stopTask;
    private Task? _stoppingTask;
    private Task? _stoppedTask;

    public Task StartAsync(CancellationToken cancellationToken) =>
        RunOnCleanContext(inner.StartAsync, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_stopGate)
        {
            return _stopTask ??= InvokeOnce(inner.StopAsync, cancellationToken);
        }
    }

    public Task StartingAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? RunOnCleanContext(lifecycle.StartingAsync, cancellationToken)
            : Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? RunOnCleanContext(lifecycle.StartedAsync, cancellationToken)
            : Task.CompletedTask;

    // Stop lifecycle is not flow-suppressed: stop methods typically signal cancellation
    // and await shutdown rather than spawning new long-running background work, so
    // context capture during Stop is not the span-leak vector that Start is. They are
    // once-guarded, though — concurrent Host.StopAsync calls invoke these hooks twice
    // (see the class remarks).
    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        if (inner is not IHostedLifecycleService lifecycle)
        {
            return Task.CompletedTask;
        }

        lock (_stopGate)
        {
            return _stoppingTask ??= InvokeOnce(lifecycle.StoppingAsync, cancellationToken);
        }
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        if (inner is not IHostedLifecycleService lifecycle)
        {
            return Task.CompletedTask;
        }

        lock (_stopGate)
        {
            return _stoppedTask ??= InvokeOnce(lifecycle.StoppedAsync, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (inner is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else if (inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Forwards to the inner service's <see cref="IDisposable.Dispose"/> when implemented.
    /// Inner services that only implement <see cref="IAsyncDisposable"/> are not disposed on
    /// this synchronous path. Callers should use <see cref="DisposeAsync"/> (or
    /// <c>await using</c> on the owning factory) to release async-only resources.
    /// </remarks>
    public void Dispose()
    {
        if (inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    // Captures a synchronous throw as a faulted task so it is cached in the once-guard —
    // otherwise a second caller would re-invoke the inner stop method.
    private static Task InvokeOnce(Func<CancellationToken, Task> op, CancellationToken ct)
    {
        try
        {
            return op(ct);
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

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
