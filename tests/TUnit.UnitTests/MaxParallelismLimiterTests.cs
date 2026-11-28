using TUnit.Engine.Scheduling;

namespace TUnit.UnitTests;

public class MaxParallelismLimiterTests
{
    private static MaxParallelismLimiter CreateAdaptive(int initial = 2, int minimum = 1, int maximum = 8)
        => new(() => new ParallelismLimits(initial, minimum, maximum));

    [Test]
    public async Task Fixed_Limiter_Admits_Up_To_Its_Maximum()
    {
        var limiter = new MaxParallelismLimiter(() => 2);

        await limiter.WaitAsync(CancellationToken.None);
        await limiter.WaitAsync(CancellationToken.None);
        var third = limiter.WaitAsync(CancellationToken.None);

        await Assert.That(limiter.Limits.IsAdaptive).IsFalse();
        await Assert.That(limiter.AvailableSlots).IsEqualTo(0);
        await Assert.That(third.IsCompleted).IsFalse();

        limiter.Release();
        await third;

        await Assert.That(limiter.ReleasedSlots).IsEqualTo(1);
    }

    [Test]
    public async Task Fixed_Limiter_Cannot_Be_Resized()
    {
        var limiter = new MaxParallelismLimiter(() => 2);

        await Assert.That(limiter.SetLimit(10)).IsEqualTo(2);
        await Assert.That(limiter.SetLimit(1)).IsEqualTo(2);
        await Assert.That(limiter.AvailableSlots).IsEqualTo(2);
    }

    [Test]
    public async Task Adaptive_Limiter_Starts_At_Its_Initial_Limit()
    {
        var limiter = CreateAdaptive();

        await Assert.That(limiter.CurrentLimit).IsEqualTo(2);
        await Assert.That(limiter.AvailableSlots).IsEqualTo(2);
        await Assert.That(limiter.MaxParallelism).IsEqualTo(8);
    }

    [Test]
    public async Task Raising_The_Limit_Admits_Waiting_Tests()
    {
        var limiter = CreateAdaptive();
        await limiter.WaitAsync(CancellationToken.None);
        await limiter.WaitAsync(CancellationToken.None);
        var third = limiter.WaitAsync(CancellationToken.None);
        var fourth = limiter.WaitAsync(CancellationToken.None);

        await Assert.That(limiter.SetLimit(3)).IsEqualTo(3);

        await third;
        await Assert.That(fourth.IsCompleted).IsFalse();
    }

    [Test]
    public async Task Lowering_The_Limit_Takes_Free_Slots_At_Once()
    {
        var limiter = CreateAdaptive(initial: 4);

        limiter.SetLimit(1);

        await Assert.That(limiter.CurrentLimit).IsEqualTo(1);
        await Assert.That(limiter.AvailableSlots).IsEqualTo(1);
    }

    [Test]
    public async Task Lowering_The_Limit_Takes_Held_Slots_As_Tests_Finish()
    {
        var limiter = CreateAdaptive(initial: 3);
        await limiter.WaitAsync(CancellationToken.None);
        await limiter.WaitAsync(CancellationToken.None);
        await limiter.WaitAsync(CancellationToken.None);
        var waiting = limiter.WaitAsync(CancellationToken.None);

        limiter.SetLimit(1);

        // Two of the three running tests give their slots up; the third hands its slot on.
        limiter.Release();
        limiter.Release();
        await Assert.That(waiting.IsCompleted).IsFalse();
        await Assert.That(limiter.AvailableSlots).IsEqualTo(0);

        limiter.Release();
        await waiting;
    }

    [Test]
    public async Task Raising_The_Limit_Again_Keeps_Slots_That_Were_About_To_Be_Taken()
    {
        var limiter = CreateAdaptive(initial: 3);
        await limiter.WaitAsync(CancellationToken.None);
        await limiter.WaitAsync(CancellationToken.None);
        await limiter.WaitAsync(CancellationToken.None);

        limiter.SetLimit(1);
        limiter.SetLimit(3);

        limiter.Release();
        limiter.Release();
        limiter.Release();

        await Assert.That(limiter.AvailableSlots).IsEqualTo(3);
    }

    [Test]
    public async Task Limit_Is_Clamped_To_Its_Bounds()
    {
        var limiter = CreateAdaptive(initial: 4, minimum: 2, maximum: 8);

        await Assert.That(limiter.SetLimit(100)).IsEqualTo(8);
        await Assert.That(limiter.AvailableSlots).IsEqualTo(8);

        await Assert.That(limiter.SetLimit(0)).IsEqualTo(2);
        await Assert.That(limiter.AvailableSlots).IsEqualTo(2);
    }

    [Test]
    public async Task Concurrent_Resizes_Never_Admit_More_Tests_Than_The_Maximum()
    {
        const int maximum = 8;
        var limiter = CreateAdaptive(initial: 4, minimum: 1, maximum: maximum);
        var running = 0;
        var mostRunning = 0;
        using var stop = new CancellationTokenSource();

        // A lost slot would leave a worker waiting forever; fail the test instead.
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var resizer = Task.Run(() =>
        {
            var random = new Random(7871);
            while (!stop.IsCancellationRequested)
            {
                limiter.SetLimit(random.Next(1, maximum + 1));
            }
        });

        var tests = Enumerable.Range(0, 32).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 500; i++)
            {
                await limiter.WaitAsync(timeout.Token);
                try
                {
                    var now = Interlocked.Increment(ref running);
                    InterlockedMax(ref mostRunning, now);
                    await Task.Yield();
                }
                finally
                {
                    Interlocked.Decrement(ref running);
                    limiter.Release();
                }
            }
        })).ToArray();

        try
        {
            await Task.WhenAll(tests);
        }
        finally
        {
            stop.Cancel();
            await resizer;
        }

        await Assert.That(mostRunning).IsLessThanOrEqualTo(maximum);

        // With nothing running, every slot of the final limit is free again.
        var limit = limiter.SetLimit(maximum);
        await Assert.That(limiter.AvailableSlots).IsEqualTo(limit);
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int current;
        while ((current = Volatile.Read(ref location)) < value
               && Interlocked.CompareExchange(ref location, value, current) != current)
        {
        }
    }
}
