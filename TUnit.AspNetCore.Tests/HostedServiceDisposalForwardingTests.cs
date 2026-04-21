using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Tests that <see cref="FlowSuppressingHostedService"/> forwards <see cref="IDisposable.Dispose"/>
/// and <see cref="IAsyncDisposable.DisposeAsync"/> calls to the wrapped inner service.
/// Constructs the wrapper directly, without a host or DI container, to isolate the forwarding
/// contract from host-disposal ordering.
/// </summary>
public class HostedServiceDisposalForwardingTests
{
    [Test]
    public async Task DisposeAsync_Forwards_To_IAsyncDisposable_Inner()
    {
        var inner = new AsyncDisposableProbeHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        await ((IAsyncDisposable) wrapper).DisposeAsync();

        await Assert.That(inner.DisposeAsyncCalled).IsTrue();
    }

    [Test]
    public async Task Dispose_Forwards_To_IDisposable_Inner()
    {
        var inner = new DisposableProbeHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        ((IDisposable) wrapper).Dispose();

        await Assert.That(inner.DisposeCalled).IsTrue();
    }

    [Test]
    public async Task DisposeAsync_On_SyncOnly_Inner_Falls_Back_To_Dispose()
    {
        var inner = new DisposableProbeHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        await ((IAsyncDisposable) wrapper).DisposeAsync();

        await Assert.That(inner.DisposeCalled).IsTrue();
    }

    [Test]
    public async Task Dispose_On_AsyncOnly_Inner_Is_No_Op()
    {
        // Sync Dispose intentionally does not release an async-only inner:
        // blocking on DisposeAsync would violate the "never block on async" rule.
        // Callers with async-only inner services should use DisposeAsync.
        var inner = new AsyncDisposableProbeHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        ((IDisposable) wrapper).Dispose();

        await Assert.That(inner.DisposeAsyncCalled).IsFalse();
    }

    [Test]
    public async Task DisposeAsync_On_DualInterface_Inner_Prefers_Async_Path()
    {
        var inner = new DualDisposableProbeHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        await ((IAsyncDisposable) wrapper).DisposeAsync();

        await Assert.That(inner.DisposeAsyncCalled).IsTrue();
        await Assert.That(inner.DisposeCalled).IsFalse();
    }

    [Test]
    public async Task Dispose_On_DualInterface_Inner_Prefers_Sync_Path()
    {
        var inner = new DualDisposableProbeHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        ((IDisposable) wrapper).Dispose();

        await Assert.That(inner.DisposeCalled).IsTrue();
        await Assert.That(inner.DisposeAsyncCalled).IsFalse();
    }

    [Test]
    public async Task Wrapper_Does_Not_Throw_When_Inner_Is_Not_Disposable()
    {
        var inner = new NonDisposableProbeHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        await Assert.That(() => ((IDisposable) wrapper).Dispose()).ThrowsNothing();
        await Assert.That(async () => await ((IAsyncDisposable) wrapper).DisposeAsync()).ThrowsNothing();
    }
}

internal sealed class DisposableProbeHostedService : IHostedService, IDisposable
{
    public bool DisposeCalled { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose() => DisposeCalled = true;
}

internal sealed class AsyncDisposableProbeHostedService : IHostedService, IAsyncDisposable
{
    public bool DisposeAsyncCalled { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        DisposeAsyncCalled = true;
        return ValueTask.CompletedTask;
    }
}

internal sealed class DualDisposableProbeHostedService : IHostedService, IDisposable, IAsyncDisposable
{
    public bool DisposeCalled { get; private set; }
    public bool DisposeAsyncCalled { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose() => DisposeCalled = true;

    public ValueTask DisposeAsync()
    {
        DisposeAsyncCalled = true;
        return ValueTask.CompletedTask;
    }
}

internal sealed class NonDisposableProbeHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
