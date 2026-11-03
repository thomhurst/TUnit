using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace TUnit.AspNetCore.Tests;

public class DisposalCoordinatingHostTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Concurrent_Async_Disposal_Waits_And_Replays_The_Result(bool fail)
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        DisposalCoordinatingHost host = null!;
        var inner = new CallbackHost(async () =>
        {
            Interlocked.Increment(ref calls);
            // DI disposes the registered IHost while disposing the host's provider.
            await host.DisposeAsync();
            await release.Task;
            if (fail)
            {
                throw new InvalidOperationException("Disposal failed");
            }
        });
        host = new DisposalCoordinatingHost(inner);

        var first = host.DisposeAsync().AsTask();
        var second = host.DisposeAsync().AsTask();
        try
        {
            await Assert.That(second.IsCompleted).IsFalse();
        }
        finally
        {
            release.TrySetResult();
        }

        if (fail)
        {
            await Assert.That(async () => await first).Throws<InvalidOperationException>();
            await Assert.That(async () => await second).Throws<InvalidOperationException>();
        }
        else
        {
            await Task.WhenAll(first, second);
        }

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task Async_Disposal_Waits_For_A_Concurrent_Synchronous_Disposal()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new CallbackHost(() => Task.CompletedTask, () =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(30)))
            {
                throw new TimeoutException();
            }
        });
        var host = new DisposalCoordinatingHost(inner);
        var synchronous = Task.Run(host.Dispose);
        Task? asynchronous = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            asynchronous = host.DisposeAsync().AsTask();
            await Assert.That(asynchronous.IsCompleted).IsFalse();
        }
        finally
        {
            release.Set();
            await synchronous;
            if (asynchronous is not null)
            {
                await asynchronous;
            }
        }
    }

    private sealed class CallbackHost(Func<Task> disposeAsync, Action? dispose = null) : IHost, IAsyncDisposable
    {
        public IServiceProvider Services => throw new NotSupportedException();
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() => dispose?.Invoke();
        public ValueTask DisposeAsync() => new(disposeAsync());
    }
}
