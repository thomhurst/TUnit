using TUnit.Core.Settings;
using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

// [NotInParallel] because tests mutate static TUnitSettings state;
// Before/After hooks snapshot and restore values so test order doesn't matter.
[NotInParallel]
public class TUnitSettingsTests
{
    private TimeSpan _savedTestTimeout;
    private TimeSpan _savedHookTimeout;
    private TimeSpan _savedForcefulExitTimeout;
    private TimeSpan _savedProcessExitHookDelay;
    private int? _savedMaximumParallelTests;
    private bool _savedDetailedStackTrace;
    private bool _savedFailFast;

    [Before(HookType.Test)]
    public void SnapshotSettings()
    {
        _savedTestTimeout = TUnitSettings.Default.Timeouts.DefaultTestTimeout;
        _savedHookTimeout = TUnitSettings.Default.Timeouts.DefaultHookTimeout;
        _savedForcefulExitTimeout = TUnitSettings.Default.Timeouts.ForcefulExitTimeout;
        _savedProcessExitHookDelay = TUnitSettings.Default.Timeouts.ProcessExitHookDelay;
        _savedMaximumParallelTests = TUnitSettings.Default.Parallelism.MaximumParallelTests;
        _savedDetailedStackTrace = TUnitSettings.Default.Display.DetailedStackTrace;
        _savedFailFast = TUnitSettings.Default.Execution.FailFast;
    }

    [After(HookType.Test)]
    public void RestoreSettings()
    {
        TUnitSettings.Default.Timeouts.DefaultTestTimeout = _savedTestTimeout;
        TUnitSettings.Default.Timeouts.DefaultHookTimeout = _savedHookTimeout;
        TUnitSettings.Default.Timeouts.ForcefulExitTimeout = _savedForcefulExitTimeout;
        TUnitSettings.Default.Timeouts.ProcessExitHookDelay = _savedProcessExitHookDelay;
        TUnitSettings.Default.Parallelism.MaximumParallelTests = _savedMaximumParallelTests;
        TUnitSettings.Default.Display.DetailedStackTrace = _savedDetailedStackTrace;
        TUnitSettings.Default.Execution.FailFast = _savedFailFast;
    }

    [Test]
    public async Task Defaults_Are_Correct()
    {
        await Assert.That(TUnitSettings.Default.Timeouts.DefaultTestTimeout).IsEqualTo(TimeSpan.FromMinutes(30));
        await Assert.That(TUnitSettings.Default.Timeouts.DefaultHookTimeout).IsEqualTo(TimeSpan.FromMinutes(5));
        await Assert.That(TUnitSettings.Default.Timeouts.ForcefulExitTimeout).IsEqualTo(TimeSpan.FromSeconds(30));
        await Assert.That(TUnitSettings.Default.Timeouts.ProcessExitHookDelay).IsEqualTo(TimeSpan.FromMilliseconds(500));
        await Assert.That(TUnitSettings.Default.Parallelism.MaximumParallelTests).IsNull();
        await Assert.That(TUnitSettings.Default.Display.DetailedStackTrace).IsFalse();
        await Assert.That(TUnitSettings.Default.Execution.FailFast).IsFalse();
    }

    [Test]
    public async Task Settings_Can_Be_Modified()
    {
        TUnitSettings.Default.Timeouts.DefaultTestTimeout = TimeSpan.FromMinutes(10);
        await Assert.That(TUnitSettings.Default.Timeouts.DefaultTestTimeout).IsEqualTo(TimeSpan.FromMinutes(10));
    }

    // Covers TestCoordinator's `test.Timeout ?? TUnitSettings...ExplicitDefaultTestTimeout` fallback:
    // when the user never assigns DefaultTestTimeout, tests without [Timeout] skip the
    // TimeoutHelper wrapper entirely (the right-hand side of the coalesce is null).
    [Test]
    public async Task ExplicitDefaultTestTimeout_Is_Null_When_Unset()
    {
        // A fresh TimeoutSettings instance models the pristine "user never assigned
        // DefaultTestTimeout" state — SnapshotSettings/RestoreSettings on the shared
        // TUnitSettings.Default cannot un-assign the backing field once any test has set it.
        var freshSettings = new TimeoutSettings();

        await Assert.That(freshSettings.ExplicitDefaultTestTimeout).IsNull();
        await Assert.That(freshSettings.DefaultTestTimeout).IsEqualTo(TimeSpan.FromMinutes(30));
    }

    [Test]
    public async Task ExplicitDefaultTestTimeout_Returns_Assigned_Value()
    {
        TUnitSettings.Default.Timeouts.DefaultTestTimeout = TimeSpan.FromMilliseconds(200);

        await Assert.That(TUnitSettings.Default.Timeouts.ExplicitDefaultTestTimeout)
            .IsEqualTo(TimeSpan.FromMilliseconds(200));
    }

    // End-to-end proof of the fallback: feeding ExplicitDefaultTestTimeout into the same
    // TimeoutHelper that TestCoordinator uses must cause a hanging test body to fail with
    // TimeoutException. Mirrors TestCoordinator's call site verbatim for this branch.
    [Test]
    public async Task Configured_Default_Timeout_Fires_On_Hanging_Test()
    {
        TUnitSettings.Default.Timeouts.DefaultTestTimeout = TimeSpan.FromMilliseconds(200);

        var testTimeout = TUnitSettings.Default.Timeouts.ExplicitDefaultTestTimeout;
        await Assert.That(testTimeout).IsNotNull();

        // Ignore the passed token so TimeoutHelper's timeout branch wins the race
        // (a cooperative Task.Delay(ct) would throw TaskCanceledException first).
        // A private CTS scoped to this test lets us cancel the delay on exit instead
        // of leaking a 30s Task to process end.
        using var hangCts = new CancellationTokenSource();
        try
        {
            await Assert.That(async () =>
                await TimeoutHelper.ExecuteWithTimeoutAsync(
                    _ => Task.Delay(TimeSpan.FromSeconds(30), hangCts.Token),
                    testTimeout!.Value,
                    CancellationToken.None))
                .Throws<TimeoutException>();
        }
        finally
        {
            hangCts.Cancel();
        }
    }
}
