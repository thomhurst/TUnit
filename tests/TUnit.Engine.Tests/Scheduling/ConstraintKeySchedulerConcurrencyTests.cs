using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Scheduling;

/// <summary>
/// Engine tests that validate ConstraintKeyScheduler handles high contention scenarios
/// without deadlocks. Invokes TUnit.TestProject.ConstraintKeyStressTests to ensure:
/// 1. Many concurrent tests with overlapping constraint keys complete successfully
/// 2. No deadlocks occur under high contention
/// 3. Tests complete within reasonable timeout
/// </summary>
[Retry(3)]
public class ConstraintKeySchedulerConcurrencyTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Repeat(5)]
    public async Task HighContention_WithOverlappingConstraints_CompletesWithoutDeadlock()
    {
        // This test establishes baseline behavior before optimization
        // It runs tests with overlapping constraint keys to verify the scheduler
        // can handle high contention without deadlocking

        // Backstop against a wedged child process. A deadlocked test fails on its own 30 s [Timeout]
        // inside ConstraintKeyStressTests, so this budget must also cover process start-up and
        // reflection discovery of the whole TUnit.TestProject assembly, which alone can exceed
        // 30 s on a loaded Windows runner (#7166).
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        await RunTestsWithFilter("/*/*/ConstraintKeyStressTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(30), // 10 tests × 3 runs (Repeat(2))
            result => result.ResultSummary.Counters.Passed.ShouldBe(30),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ],
        new RunOptions().WithForcefulCancellationToken(cts.Token));
    }
}
