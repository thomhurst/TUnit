using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/6339.
/// <para>
/// Minimal-hosting SUTs park <c>app.Run()</c> in <c>WaitForShutdownAsync</c>, which calls
/// <c>Host.StopAsync</c> when <c>ApplicationStopping</c> fires — concurrently with the
/// <c>Host.StopAsync</c> from <c>WebApplicationFactory.DisposeAsync</c> that triggered it.
/// Every hosted service's stop then runs twice in parallel, breaking services with
/// non-thread-safe shutdown (Rebus' bus dispose throws <see cref="ObjectDisposedException"/>).
/// <see cref="FlowSuppressingHostedService"/> must absorb the duplicate calls.
/// </para>
/// </summary>
public class HostedServiceStopOnceTests
{
    [Test]
    public async Task Concurrent_StopAsync_Invokes_Inner_Once()
    {
        var inner = new CountingHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        var gate = new TaskCompletionSource();
        var callers = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(async () =>
            {
                await gate.Task;
                await wrapper.StopAsync(CancellationToken.None);
            }))
            .ToArray();
        gate.SetResult();
        await Task.WhenAll(callers);

        await Assert.That(inner.StopCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Sequential_StopAsync_Invokes_Inner_Once()
    {
        var inner = new CountingHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        await wrapper.StopAsync(CancellationToken.None);
        await wrapper.StopAsync(CancellationToken.None);

        await Assert.That(inner.StopCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Duplicate_Stop_Callers_Observe_The_Same_Task()
    {
        var inner = new CountingHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        var first = wrapper.StopAsync(CancellationToken.None);
        var second = wrapper.StopAsync(CancellationToken.None);

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task Duplicate_Stop_Caller_Observes_Its_Own_Cancellation_Token()
    {
        var inner = new BlockingStopHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        var first = wrapper.StopAsync(CancellationToken.None);
        await inner.Entered;

        using var cancellationTokenSource = new CancellationTokenSource();
        var second = wrapper.StopAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        await Assert.That(async () => await second).Throws<OperationCanceledException>();
        await Assert.That(inner.StopCalls).IsEqualTo(1);

        inner.Release();
        await first;
    }

    [Test]
    public async Task Owning_Stop_Caller_Awaits_Inner_After_Token_Cancellation()
    {
        var inner = new BlockingStopHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        using var cancellationTokenSource = new CancellationTokenSource();
        var stop = wrapper.StopAsync(cancellationTokenSource.Token);
        await inner.Entered;

        try
        {
            cancellationTokenSource.Cancel();
            await Assert.That(stop.IsCompleted).IsFalse();
        }
        finally
        {
            inner.Release();
        }

        await stop;
    }

    [Test]
    public async Task Duplicate_Stop_Caller_Does_Not_Wait_For_Synchronous_Startup()
    {
        using var inner = new SynchronouslyBlockingStopHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        var first = Task.Run(() => wrapper.StopAsync(CancellationToken.None));
        inner.WaitUntilEntered();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        try
        {
            var second = Task.Run(() => wrapper.StopAsync(cancellationTokenSource.Token));
            await Assert.That(async () => await second.WaitAsync(TimeSpan.FromSeconds(1)))
                .Throws<OperationCanceledException>();
        }
        finally
        {
            inner.Release();
        }

        await first;
        await Assert.That(inner.StopCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Synchronously_Throwing_Stop_Is_Cached_Not_Reinvoked()
    {
        var inner = new SyncThrowingStopHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        await Assert.That(async () => await wrapper.StopAsync(CancellationToken.None))
            .Throws<InvalidOperationException>();
        await Assert.That(async () => await wrapper.StopAsync(CancellationToken.None))
            .Throws<InvalidOperationException>();

        await Assert.That(inner.StopCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Lifecycle_Stopping_And_Stopped_Are_Once_Guarded()
    {
        var inner = new CountingLifecycleHostedService();
        var wrapper = new FlowSuppressingHostedService(inner);

        await wrapper.StoppingAsync(CancellationToken.None);
        await wrapper.StoppingAsync(CancellationToken.None);
        await wrapper.StoppedAsync(CancellationToken.None);
        await wrapper.StoppedAsync(CancellationToken.None);

        await Assert.That(inner.StoppingCalls).IsEqualTo(1);
        await Assert.That(inner.StoppedCalls).IsEqualTo(1);
    }
}

internal class CountingHostedService : IHostedService
{
    private int _stopCalls;

    public int StopCalls => _stopCalls;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _stopCalls);

        // Keep the stop in flight briefly so concurrent duplicate callers would
        // overlap it rather than arrive after completion.
        await Task.Delay(50, CancellationToken.None);
    }
}

internal sealed class SyncThrowingStopHostedService : IHostedService
{
    private int _stopCalls;

    public int StopCalls => _stopCalls;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _stopCalls);
        throw new InvalidOperationException("stop failed synchronously");
    }
}

internal sealed class BlockingStopHostedService : IHostedService
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _stopCalls;

    public Task Entered => _entered.Task;
    public int StopCalls => _stopCalls;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _stopCalls);
        _entered.SetResult();
        await _release.Task;
    }

    public void Release() => _release.SetResult();
}

internal sealed class SynchronouslyBlockingStopHostedService : IHostedService, IDisposable
{
    private readonly ManualResetEventSlim _entered = new();
    private readonly ManualResetEventSlim _release = new();
    private int _stopCalls;

    public int StopCalls => _stopCalls;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _stopCalls);
        _entered.Set();
        _release.Wait();
        return Task.CompletedTask;
    }

    public void WaitUntilEntered()
    {
        if (!_entered.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("StopAsync was not entered within five seconds.");
        }
    }
    public void Release() => _release.Set();

    public void Dispose()
    {
        _entered.Dispose();
        _release.Dispose();
    }
}

internal sealed class CountingLifecycleHostedService : IHostedService, IHostedLifecycleService
{
    private int _stoppingCalls;
    private int _stoppedCalls;

    public int StoppingCalls => _stoppingCalls;
    public int StoppedCalls => _stoppedCalls;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _stoppingCalls);
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _stoppedCalls);
        return Task.CompletedTask;
    }
}
