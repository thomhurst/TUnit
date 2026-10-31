using Shouldly;
using TUnit.Engine.Tests.Enums;
using TUnit.Engine.Tests.Extensions;

namespace TUnit.Engine.Tests;

/// <summary>
/// Verifies that a late ITestEndEventReceiver sees Failed when the test body passes but an
/// [After(Test)] hook throws. TUnit.Playwright's [CaptureOnFailure] relies on this to keep
/// screenshots and traces for teardown failures (#7289).
/// </summary>
public class LateReceiverSeesTeardownFailure7289Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        // FindFile searches the whole repository, so match only this run's files.
        var runId = Guid.NewGuid().ToString("N");
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7289/LateReceiverSeesTeardownFailure/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                // The interim Failed result is only for late receivers. The reported result is
                // recorded afterwards and must still carry the teardown exception.
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain("Teardown failed on purpose"),
                _ => FindFile(x => x.Name == $"{runId}-LateReceiverSeesTeardownFailure-Failed.txt").AssertExists(),
                _ => FindFile(x => x.Name.StartsWith(runId, StringComparison.Ordinal) && !x.Name.EndsWith("-Failed.txt", StringComparison.Ordinal)).ShouldBeNull()
            ],
            new RunOptions().WithEnvironmentVariable("TUNIT_7289_RUN_ID", runId));
    }
}
