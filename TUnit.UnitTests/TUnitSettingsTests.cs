using TUnit.Core.Settings;

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
}
