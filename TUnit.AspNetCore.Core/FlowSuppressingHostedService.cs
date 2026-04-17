using Microsoft.Extensions.Hosting;

namespace TUnit.AspNetCore;

/// <summary>
/// Wraps an <see cref="IHostedService"/> so its <see cref="IHostedService.StartAsync"/>
/// runs with <see cref="ExecutionContext.SuppressFlow"/> active. Background tasks
/// spawned inside <c>StartAsync</c> capture a clean <see cref="ExecutionContext"/>,
/// so activities they later emit do not inherit the test's ambient
/// <see cref="System.Diagnostics.Activity.Current"/> as their parent.
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
        Suppressed(inner.StartAsync, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) =>
        inner.StopAsync(cancellationToken);

    public Task StartingAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? Suppressed(lifecycle.StartingAsync, cancellationToken)
            : Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? Suppressed(lifecycle.StartedAsync, cancellationToken)
            : Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? lifecycle.StoppingAsync(cancellationToken)
            : Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) =>
        inner is IHostedLifecycleService lifecycle
            ? lifecycle.StoppedAsync(cancellationToken)
            : Task.CompletedTask;

    private static Task Suppressed(Func<CancellationToken, Task> op, CancellationToken ct)
    {
        using var _ = ExecutionContext.SuppressFlow();
        return op(ct);
    }
}
