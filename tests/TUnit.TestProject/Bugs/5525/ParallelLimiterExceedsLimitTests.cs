using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5525;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/5525
/// ParallelLimiter with Limit=2 must cap concurrency at 2 even when
/// a [DependsOn] test triggers dependency execution.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ParallelLimiterExceedsLimitTests
{
    private static int s_concurrent;
    private static int s_peak;

    [Before(Class)]
    public static void Reset()
    {
        s_concurrent = 0;
        s_peak = 0;
    }

    [Test, ParallelLimiter<Limit2>] public Task T01() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T02() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T03() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T04() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T05() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T06() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T07() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T08() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T09() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T10() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T11() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T12() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T13() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T14() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T15() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T16() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T17() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T18() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T19() => MeasureAsync();
    [Test, ParallelLimiter<Limit2>] public Task T20() => MeasureAsync();

    [Test]
    [DependsOn(nameof(T01)), DependsOn(nameof(T02)),
     DependsOn(nameof(T03)), DependsOn(nameof(T04)),
     DependsOn(nameof(T05)), DependsOn(nameof(T06)),
     DependsOn(nameof(T07)), DependsOn(nameof(T08)),
     DependsOn(nameof(T09)), DependsOn(nameof(T10)),
     DependsOn(nameof(T11)), DependsOn(nameof(T12)),
     DependsOn(nameof(T13)), DependsOn(nameof(T14)),
     DependsOn(nameof(T15)), DependsOn(nameof(T16)),
     DependsOn(nameof(T17)), DependsOn(nameof(T18)),
     DependsOn(nameof(T19)), DependsOn(nameof(T20))]
    public async Task PeakIsAtMostLimit()
    {
        await Assert.That(s_peak).IsLessThanOrEqualTo(2);
    }

    /// <summary>
    /// Same assertion but this test also carries the limiter — verifies the
    /// two-phase acquire (deps first, then limiter) works when the depending
    /// test shares the same limiter as its dependencies.
    /// </summary>
    [Test, ParallelLimiter<Limit2>]
    [DependsOn(nameof(T01)), DependsOn(nameof(T02)),
     DependsOn(nameof(T03)), DependsOn(nameof(T04)),
     DependsOn(nameof(T05)), DependsOn(nameof(T06)),
     DependsOn(nameof(T07)), DependsOn(nameof(T08)),
     DependsOn(nameof(T09)), DependsOn(nameof(T10)),
     DependsOn(nameof(T11)), DependsOn(nameof(T12)),
     DependsOn(nameof(T13)), DependsOn(nameof(T14)),
     DependsOn(nameof(T15)), DependsOn(nameof(T16)),
     DependsOn(nameof(T17)), DependsOn(nameof(T18)),
     DependsOn(nameof(T19)), DependsOn(nameof(T20))]
    public async Task PeakIsAtMostLimitWithSameLimiterOnDependent()
    {
        await Assert.That(s_peak).IsLessThanOrEqualTo(2);
    }

    private static async Task MeasureAsync()
    {
        var current = Interlocked.Increment(ref s_concurrent);

        int old;
        do
        {
            old = s_peak;
            if (current <= old)
            {
                break;
            }
        }
        while (Interlocked.CompareExchange(ref s_peak, current, old) != old);

        await Task.Delay(50).ConfigureAwait(false);

        Interlocked.Decrement(ref s_concurrent);
    }
}

internal sealed class Limit2 : IParallelLimit
{
    public int Limit => 2;
}
