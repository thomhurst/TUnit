using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Integration test for PR #5728 Bug 2: a timeout that fires via
/// <c>TUnitSettings.Default.Timeouts.DefaultTestTimeout</c> (instead of a <c>[Timeout]</c>
/// attribute) must surface as a timeout failure end-to-end, not a generic error.
///
/// Before the fix, <c>TestDetails.Timeout</c> was left null when the timeout was resolved
/// from settings, so <c>TUnitMessageBus.GetFailureStateProperty</c> fell through to
/// <c>ErrorTestNodeStateProperty</c> — which caused JUnit/GitHub/HTML reporters to label
/// real timeouts as errors.
/// </summary>
public class DefaultTimeoutClassificationTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Hanging_Test_Fails_With_Timeout_Message_When_Only_DefaultTestTimeout_Is_Set()
    {
        // The opt-in env var lets the corresponding [Before(TestDiscovery)] hook in TestProject
        // configure DefaultTestTimeout to 200ms for this run only. Without the env var the hook
        // is a no-op, so the setting never leaks into the broader test project.
        await RunTestsWithFilter(
            "/*/*/DefaultTimeoutClassificationTests/Hanging_Test_With_DefaultTestTimeout_Should_Timeout",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                // Test body sleeps 10s but the 200ms default timeout must fire well before that —
                // bounds guard against the timeout silently not firing (would sleep the full 10s).
                result => TimeSpan.Parse(result.Results[0].Duration).ShouldBeLessThan(TimeSpan.FromSeconds(5)),
                result =>
                {
                    // Confirm the failure was classified as a timeout rather than a generic error.
                    // "Timed out" appears in TimeoutTestNodeStateProperty's explanation; a generic
                    // error path would surface the raw exception message without the "timed out" phrase.
                    var errorMessage = result.Results.First().Output?.ErrorInfo?.Message;
                    errorMessage.ShouldNotBeNull("Expected an error message for the timed-out test");
                    errorMessage!.ToLowerInvariant().ShouldContain("timed out");
                }
            ],
            new RunOptions().WithEnvironmentVariable("TUNIT_BUG5728_DEFAULT_TIMEOUT", "1"));
    }
}
