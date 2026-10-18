using CliWrap.Buffered;
using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Scheduling;

/// <summary>
/// With --fail-fast and --maximum-parallel-tests 1, the failing keyed test cancels the keyed
/// tests queued behind it, so the scheduling phases throw. The After(TestSession) hook must
/// still be awaited, before shared fixtures are disposed, instead of being left running in the
/// background while the session tears down. Fail-fast must also end the run as a failed session
/// rather than crash the test host with an unhandled cancellation.
/// </summary>
public class KeyedNotInParallelFailFastTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string MarkerVariable = "TUNIT_7074_SESSION_HOOK_MARKER_PATH";

    [Test]
    public async Task FailFast_WithWaitingKeyedTests_AwaitsSessionHooksBeforeFixtureDisposal()
    {
        var markerPath = Path.Combine(Path.GetTempPath(), $"tunit-7074-session-hook-{Guid.NewGuid():N}.txt");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var process = new ProcessResultCapture();

        try
        {
            await RunTestsWithFilter("/*/*/KeyedNotInParallelFailFast7074/*",
            [
                _ => process.AssertEndedWithoutCrash(),
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
                .WithForcefulCancellationToken(cts.Token)
                .Capture(process));
        }
        finally
        {
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
            }
        }
    }

    private const string DynamicMarkerVariable = "TUNIT_7074_DYNAMIC_MARKER_PATH";

    [Test]
    public async Task FailFast_WithRunningDynamicTest_AwaitsDynamicTestBeforeFixtureDisposal()
    {
        var markerPath = Path.Combine(Path.GetTempPath(), $"tunit-7074-dynamic-{Guid.NewGuid():N}.txt");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var process = new ProcessResultCapture();

        try
        {
            await RunTestsWithFilter("/*/*/KeyedNotInParallelFailFastDynamic7074/*",
            [
                _ => process.AssertEndedWithoutCrash(),
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Failed.ShouldBeGreaterThanOrEqualTo(1),
                _ => File.Exists(markerPath).ShouldBeTrue($"Marker file '{markerPath}' was not written: the session ended before the dynamic test finished"),
                _ => File.ReadAllText(markerPath).ShouldBe("FixtureDisposedDuringDynamicTest=False"),
            ],
            new RunOptions()
                .WithArgument("--fail-fast")
                .WithArgument("--maximum-parallel-tests")
                .WithArgument("2")
                .WithEnvironmentVariable(DynamicMarkerVariable, markerPath)
                .WithForcefulCancellationToken(cts.Token)
                .Capture(process));
        }
        finally
        {
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
            }
        }
    }

    /// <summary>
    /// When the failing test is the last one to run, the phases finish without throwing, so the
    /// scheduler awaits the dynamic test queue processor on its normal path. Its wait on the
    /// queue must end cleanly on the cancelled token instead of surfacing an
    /// OperationCanceledException that crashes the host.
    /// </summary>
    [Test]
    public async Task FailFast_WhenFailingTestRunsLast_EndsSessionWithoutCrash()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var process = new ProcessResultCapture();

        await RunTestsWithFilter("/*/*/KeyedNotInParallelFailFast7074/Fails",
        [
            _ => process.AssertEndedWithoutCrash(),
            result => result.ResultSummary.Outcome.ShouldBe("Failed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
        ],
        new RunOptions()
            .WithArgument("--fail-fast")
            .WithForcefulCancellationToken(cts.Token)
            .Capture(process));
    }
}

internal sealed class ProcessResultCapture
{
    public BufferedCommandResult? Result { get; set; }

    public void AssertEndedWithoutCrash()
    {
        var result = Result.ShouldNotBeNull();
        var output = result.StandardOutput + result.StandardError;

        // The runtime's crash banner for an exception that escaped Main.
        output.ShouldNotContain("Unhandled exception. ");

        // MTP's "at least one test failed" or "test session failed" codes, not a runtime crash.
        result.ExitCode.ShouldBeOneOf(2, 10);
    }
}

internal static class ProcessResultCaptureExtensions
{
    public static RunOptions Capture(this RunOptions runOptions, ProcessResultCapture capture)
    {
        runOptions.OnExecutingDelegates.Add(async commandTask => capture.Result = await commandTask);
        return runOptions;
    }
}
