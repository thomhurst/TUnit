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
    private const int CancellationDelaySeconds = 5;
    private const int MaxExpectedDurationSeconds = 30;

    [Test, Timeout(30_000)]
    public async Task GracefulCancellation_ShouldTerminateTestBeforeTimeout(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(CancellationDelaySeconds));
        await RunTestsWithFilter(
            "/*/*/CanCancelTests/*",
            [
                // A cancelled test is reported as "Failed" in TRX because it was terminated before completion.
                // This is the expected behavior - the test did not pass, so it's marked as failed.
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => TimeSpan.Parse(result.Duration).ShouldBeLessThan(TimeSpan.FromSeconds(MaxExpectedDurationSeconds))
            ],
            new RunOptions().WithGracefulCancellationToken(cts.Token));
    }
}
