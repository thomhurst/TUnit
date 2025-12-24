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
    private const int ForcefulCancellationDelaySeconds = 15;
    private const int MaxExpectedDurationSeconds = 30;
    // Test timeout must be higher than MaxExpectedDurationSeconds to allow for subprocess startup and assertions
    private const int TestTimeoutMs = 60_000;

    [Test, Timeout(TestTimeoutMs)]
    public async Task GracefulCancellation_ShouldTerminateTestBeforeTimeout(CancellationToken ct)
    {
        // Graceful cancellation first (SIGINT), then forceful (SIGKILL) as backup
        using var gracefulCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var forcefulCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        gracefulCts.CancelAfter(TimeSpan.FromSeconds(GracefulCancellationDelaySeconds));
        forcefulCts.CancelAfter(TimeSpan.FromSeconds(ForcefulCancellationDelaySeconds));

        await RunTestsWithFilter(
            "/*/*/CanCancelTests/*",
            [
                // A cancelled test is reported as "Failed" in TRX because it was terminated before completion.
                // This is the expected behavior - the test did not pass, so it's marked as failed.
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => TimeSpan.Parse(result.Duration).ShouldBeLessThan(TimeSpan.FromSeconds(MaxExpectedDurationSeconds))
            ],
            new RunOptions()
                .WithGracefulCancellationToken(gracefulCts.Token)
                .WithForcefulCancellationToken(forcefulCts.Token));
    }
}
