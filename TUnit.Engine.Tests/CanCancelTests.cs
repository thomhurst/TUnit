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
    [Test, Timeout(30_000)]
    public async Task Test(CancellationToken ct)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await RunTestsWithFilter(
            "/*/*/CanCancelTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => TimeSpan.Parse(result.Duration).ShouldBeLessThan(TimeSpan.FromSeconds(30))
            ],
            new RunOptions().WithGracefulCancellationToken(cts.Token));
    }
}
