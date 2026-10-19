using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Scheduling;

/// <summary>
/// #7119: on a --fail-fast run every selected test must get exactly one result node. Tests that
/// fail-fast stops before they start are reported as skipped, and the failure that triggered
/// fail-fast is not reported a second time as an "Unhandled exception" node when the
/// unconstrained and keyed phases were running at the same time.
/// </summary>
public class FailFastReportingTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private static readonly string[] TestNames =
        ["Fails", "Parallel1", "Parallel2", "Keyed1", "Keyed2", "Global1", "Global2"];

    [Test]
    public async Task FailFast_ReportsEveryTestOnce_AndCancelledTestsAsSkipped()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var process = new ProcessResultCapture();

        await RunTestsWithFilter("/*/*/FailFastReporting7119/*",
        [
            _ => process.AssertEndedWithoutCrash(),
            result => result.ResultSummary.Outcome.ShouldBe("Failed"),
            result => result.Results.ShouldNotContain(
                x => x.TestName!.StartsWith("Unhandled exception", StringComparison.Ordinal),
                "The fail-fast failure was reported a second time as an 'Unhandled exception' node"),
            result => result.Results.Select(x => x.TestName).OrderBy(x => x).ShouldBe(TestNames.OrderBy(x => x)),
            result => result.Results.Single(x => x.TestName == "Fails").Outcome.ShouldBe("Failed"),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            // Fails runs no later than second, so fail-fast stops at least the two global
            // [NotInParallel] tests and the tests still waiting for the single slot.
            result => result.Results.Where(x => x.TestName != "Fails")
                .ShouldAllBe(x => x.Outcome == "Passed" || x.Outcome == "NotExecuted"),
            result => result.Results.Count(x => x.Outcome == "NotExecuted").ShouldBeGreaterThanOrEqualTo(4),
            result => result.Results.Where(x => x.TestName!.StartsWith("Global", StringComparison.Ordinal))
                .ShouldAllBe(x => x.Outcome == "NotExecuted"),
        ],
        new RunOptions()
            .WithArgument("--fail-fast")
            .WithArgument("--maximum-parallel-tests")
            .WithArgument("1")
            .WithForcefulCancellationToken(cts.Token)
            .Capture(process));
    }
}
