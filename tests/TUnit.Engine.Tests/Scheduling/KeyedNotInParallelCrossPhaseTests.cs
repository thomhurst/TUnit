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
    // The regression is detected inside the child process: Repro5700 and CrossKeyOverlap5700
    // wait on a rendezvous with a 60 s deadline and fail with a test failure if it never fires.
    // This budget is only a backstop against a wedged child process, so it must exceed that
    // deadline plus process start-up and reflection discovery of the whole TUnit.TestProject
    // assembly. A 30 s budget killed healthy runs on loaded Windows runners (#7166) and would
    // also have masked a real regression as "process terminated" instead of a test failure.
    private static readonly TimeSpan ProcessBudget = TimeSpan.FromMinutes(3);

    [Test]
    public async Task UnconstrainedTest_RunsAlongsideKeyedNotInParallelTest()
    {
        using var cts = new CancellationTokenSource(ProcessBudget);

        await RunTestsWithFilter("/*/*/Repro5700/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(3),
            result => result.ResultSummary.Counters.Passed.ShouldBe(3),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0),
        ],
        new RunOptions().WithForcefulCancellationToken(cts.Token));
    }

    [Test]
    public async Task KeyedNotInParallel_DifferentKeys_RunInParallel()
    {
        using var cts = new CancellationTokenSource(ProcessBudget);

        await RunTestsWithFilter("/*/*/CrossKeyOverlap5700/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(2),
            result => result.ResultSummary.Counters.Passed.ShouldBe(2),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0),
        ],
        new RunOptions().WithForcefulCancellationToken(cts.Token));
    }
}
