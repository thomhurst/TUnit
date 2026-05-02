using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Scheduling;

/// <summary>
/// Regression for https://github.com/thomhurst/TUnit/issues/5800.
///
/// A Parallel-bucket test with <c>[DependsOn(LonelyTest)]</c> previously triggered
/// the global-<c>[NotInParallel]</c> LonelyTest mid–parallel-phase via dependency
/// recursion in <c>TestRunner</c>, where it ran alongside parallel siblings. The
/// runtime <c>NotInParallelLock</c> drains readers before LonelyTest's body runs,
/// restoring the documented "completely alone" semantic.
/// </summary>
public class GlobalNotInParallelDependsOnTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task GlobalNotInParallel_RunsAlone_EvenWhenSurfacedByDependsOn()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        await RunTestsWithFilter("/*/*/Repro5800/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(7),
            result => result.ResultSummary.Counters.Passed.ShouldBe(7),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0),
        ],
        new RunOptions().WithForcefulCancellationToken(cts.Token));
    }
}
