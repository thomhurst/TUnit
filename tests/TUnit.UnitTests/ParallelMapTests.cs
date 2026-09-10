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
        const int count = 10_000;
        var invoked = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ParallelMap.ForParallelAsync(
                count,
                async i =>
                {
                    Interlocked.Increment(ref invoked);

                    if (i == 0)
                    {
                        throw new InvalidOperationException("fail fast");
                    }

                    await Task.Yield();
                    return i;
                },
                Environment.ProcessorCount));

        // The parked cursor must prevent the full list from being processed.
        // In-flight items may still complete, so allow generous slack.
        await Assert.That(invoked).IsLessThan(count);
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
}
