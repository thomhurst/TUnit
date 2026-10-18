using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Scheduling;

/// <summary>
/// With --fail-fast and --maximum-parallel-tests 1, the failing keyed test cancels the keyed
/// tests still waiting for a parallelism slot, so the scheduling phases throw. The
/// After(TestSession) hook must still be awaited, before shared fixtures are disposed,
/// instead of being left running in the background while the session tears down.
/// </summary>
public class KeyedNotInParallelFailFastTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string MarkerVariable = "TUNIT_7074_SESSION_HOOK_MARKER_PATH";

    [Test]
    public async Task FailFast_WithWaitingKeyedTests_AwaitsSessionHooksBeforeFixtureDisposal()
    {
        var markerPath = Path.Combine(Path.GetTempPath(), $"tunit-7074-session-hook-{Guid.NewGuid():N}.txt");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        try
        {
            await RunTestsWithFilter("/*/*/KeyedNotInParallelFailFast7074/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Failed.ShouldBeGreaterThanOrEqualTo(1),
                // Fails runs first and holds the only slot, so fail-fast must cancel at least one
                // Waits test that was still queued for it.
                result => result.ResultSummary.Counters.Passed.ShouldBeLessThan(4),
                _ => File.Exists(markerPath).ShouldBeTrue($"Marker file '{markerPath}' was not written: the After(TestSession) hook was not awaited"),
                _ => File.ReadAllText(markerPath).ShouldBe("FixtureDisposedDuringHook=False"),
            ],
            new RunOptions()
                .WithArgument("--fail-fast")
                .WithArgument("--maximum-parallel-tests")
                .WithArgument("1")
                .WithEnvironmentVariable(MarkerVariable, markerPath)
                .WithForcefulCancellationToken(cts.Token));
        }
        finally
        {
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
            }
        }
    }
}
