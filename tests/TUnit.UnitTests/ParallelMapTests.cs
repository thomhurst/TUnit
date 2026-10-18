using System.Threading.Tasks.Sources;
using TUnit.Engine.Utilities;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for the internal bounded-parallelism map that backs test discovery.
/// </summary>
public class ParallelMapTests
{
    [Test]
    public async Task EmptySource_ReturnsEmptyArray()
    {
        var results = await ParallelMap.SelectParallelAsync<int, int>(
            [], x => Task.FromResult(x), Environment.ProcessorCount);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    [Arguments(1)]
    [Arguments(ParallelMap.SequentialThreshold - 1)]
    [Arguments(ParallelMap.SequentialThreshold)]
    [Arguments(ParallelMap.SequentialThreshold + 1)]
    [Arguments(100)]
    public async Task ResultsPreserveSourceOrder(int count)
    {
        var source = Enumerable.Range(0, count).ToArray();

        var results = await ParallelMap.SelectParallelAsync(
            source,
            async x =>
            {
                // Stagger completion so later items often finish before earlier ones
                await Task.Delay(x % 3);
                return x * 2;
            },
            maxDegreeOfParallelism: 4);

        await Assert.That(results.Length).IsEqualTo(count);

        for (var i = 0; i < count; i++)
        {
            await Assert.That(results[i]).IsEqualTo(i * 2);
        }
    }

    [Test]
    public async Task EveryIndexInvokedExactlyOnce()
    {
        const int count = 500;
        var invocations = new int[count];

        await ParallelMap.ForParallelAsync(
            count,
            async i =>
            {
                Interlocked.Increment(ref invocations[i]);
                await Task.Yield();
                return i;
            },
            Environment.ProcessorCount);

        for (var i = 0; i < count; i++)
        {
            await Assert.That(invocations[i]).IsEqualTo(1);
        }
    }

    [Test]
    public async Task MaxDegreeOfParallelism_IsNotExceeded()
    {
        const int count = 64;
        const int dop = 4;
        var current = 0;
        var maxObserved = 0;

        await ParallelMap.ForParallelAsync(
            count,
            async _ =>
            {
                var now = Interlocked.Increment(ref current);
                InterlockedMax(ref maxObserved, now);
                await Task.Delay(5);
                Interlocked.Decrement(ref current);
                return 0;
            },
            dop);

        await Assert.That(maxObserved).IsLessThanOrEqualTo(dop);
        await Assert.That(maxObserved).IsGreaterThan(1);
    }

    [Test]
    public async Task SelectorException_PropagatesToCaller()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ParallelMap.ForParallelAsync<int>(
                100,
                i => i == 5
                    ? throw new InvalidOperationException("boom")
                    : Task.FromResult(i),
                Environment.ProcessorCount));

        await Assert.That(exception.Message).IsEqualTo("boom");
    }

    [Test]
    public async Task SelectorException_StopsSiblingsPullingNewItems()
    {
        // Every worker takes one item and suspends on it, so the test decides the order:
        // first item 0 faults, then the siblings finish. Letting the items race instead
        // made this flaky: a worker preempted between the fault and parking the cursor
        // let its siblings process all items.
        const int count = 10_000;
        const int workers = 4;
        var invoked = 0;
        var allWorkersBusy = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var siblingsGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var faultingItem = new ObservableValueTaskSource<int>();

        var run = ParallelMap.ForParallelValueAsync(
            count,
            i =>
            {
                if (Interlocked.Increment(ref invoked) == workers)
                {
                    allWorkersBusy.SetResult();
                }

                return i == 0 ? faultingItem.ValueTask : new ValueTask<int>(WaitForGateAsync(i));
            },
            workers);

        await allWorkersBusy.Task;
        await faultingItem.Awaited;

        // The source runs continuations synchronously, so the worker's catch block has
        // parked the cursor by the time SetException returns.
        faultingItem.SetException(new InvalidOperationException("fail fast"));
        siblingsGate.SetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => run);

        // Only the items taken before the fault ran; the parked cursor stopped the rest.
        await Assert.That(invoked).IsEqualTo(workers);

        async Task<int> WaitForGateAsync(int i)
        {
            await siblingsGate.Task;
            return i;
        }
    }

    [Test]
    public async Task PreCancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ParallelMap.ForParallelAsync(
                100,
                i => Task.FromResult(i),
                Environment.ProcessorCount,
                cts.Token));
    }

    [Test]
    public async Task ForEachParallelAsync_VisitsEveryItemExactlyOnce()
    {
        const int count = 500;
        var source = Enumerable.Range(0, count).ToArray();
        var visits = new int[count];

        await ParallelMap.ForEachParallelAsync(
            source,
            async i =>
            {
                Interlocked.Increment(ref visits[i]);
                await Task.Yield();
            },
            Environment.ProcessorCount);

        for (var i = 0; i < count; i++)
        {
            await Assert.That(visits[i]).IsEqualTo(1);
        }
    }

    [Test]
    public async Task ForEachParallelAsync_ExceptionPropagates()
    {
        var source = Enumerable.Range(0, 100).ToArray();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ParallelMap.ForEachParallelAsync(
                source,
                i => i == 50 ? throw new InvalidOperationException("boom") : default,
                Environment.ProcessorCount));
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int snapshot;
        do
        {
            snapshot = Volatile.Read(ref location);
            if (value <= snapshot)
            {
                return;
            }
        } while (Interlocked.CompareExchange(ref location, value, snapshot) != snapshot);
    }

    /// <summary>
    /// A value-task source that reports when its awaiter suspends on it and, once completed,
    /// runs that awaiter's continuation synchronously on the completing thread.
    /// </summary>
    private sealed class ObservableValueTaskSource<T> : IValueTaskSource<T>
    {
        private readonly TaskCompletionSource _awaited = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private ManualResetValueTaskSourceCore<T> _core;

        public ValueTask<T> ValueTask => new(this, _core.Version);

        public Task Awaited => _awaited.Task;

        public void SetException(Exception exception) => _core.SetException(exception);

        public T GetResult(short token) => _core.GetResult(token);

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _core.OnCompleted(continuation, state, token, flags);
            _awaited.TrySetResult();
        }
    }
}
