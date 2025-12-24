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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(CancellationDelaySeconds));
        await RunTestsWithFilter(
            "/*/*/CanCancelTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => TimeSpan.Parse(result.Duration).ShouldBeLessThan(TimeSpan.FromSeconds(MaxExpectedDurationSeconds))
            ],
            new RunOptions().WithGracefulCancellationToken(cts.Token));
    }
}
