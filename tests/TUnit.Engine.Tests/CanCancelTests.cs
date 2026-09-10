using Shouldly;
using TUnit.Core.Enums;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Tests that verify test cancellation works correctly when graceful cancellation is requested.
/// Skipped on Windows because CliWrap's graceful cancellation uses GenerateConsoleCtrlEvent,
/// which doesn't work properly for subprocess control.
/// </summary>
[ExcludeOn(OS.Windows)]
public class CanCancelTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const int GracefulCancellationDelaySeconds = 5;
    private const int MaxExpectedDurationSeconds = 30;
    // Test timeout must be higher than MaxExpectedDurationSeconds to allow for subprocess startup and assertions
    private const int TestTimeoutMs = 60_000;

    [Test, Timeout(TestTimeoutMs), Explicit("Graceful cancellation via SIGINT is unreliable in CI environments")]
    public async Task GracefulCancellation_ShouldTerminateTestBeforeTimeout(CancellationToken ct)
    {
        // Send graceful cancellation (SIGINT) after delay - tests that engine reacts to cancellation
        // even for tests that don't explicitly accept a CancellationToken parameter
        using var gracefulCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        gracefulCts.CancelAfter(TimeSpan.FromSeconds(GracefulCancellationDelaySeconds));

        await RunTestsWithFilter(
            "/*/*/CanCancelTests/*",
            [
                // A cancelled test is reported as "Failed" in TRX because it was terminated before completion.
                // This is the expected behavior - the test did not pass, so it's marked as failed.
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => TimeSpan.Parse(result.Duration).ShouldBeLessThan(TimeSpan.FromSeconds(MaxExpectedDurationSeconds))
            ],
            new RunOptions().WithGracefulCancellationToken(gracefulCts.Token));
    }
}
