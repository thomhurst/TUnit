using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Scheduling;

/// <summary>
/// Regression for https://github.com/thomhurst/TUnit/discussions/5700.
/// An unconstrained [Test] must run concurrently with [Test, NotInParallel("key")] tests
/// in the same class — keyed NotInParallel only blocks tests sharing a key, it must not
/// serialize against the rest of the suite.
/// </summary>
public class KeyedNotInParallelCrossPhaseTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task UnconstrainedTest_RunsAlongsideKeyedNotInParallelTest()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await RunTestsWithFilter("/*/*/Repro5700/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(3),
            result => result.ResultSummary.Counters.Passed.ShouldBe(3),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0),
        ],
        new RunOptions().WithForcefulCancellationToken(cts.Token));
    }
}
