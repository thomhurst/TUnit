using TUnit.Core.Settings;

namespace TUnit.UnitTests;

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
        _savedTestTimeout = TUnitSettings.Timeouts.DefaultTestTimeout;
        _savedHookTimeout = TUnitSettings.Timeouts.DefaultHookTimeout;
        _savedForcefulExitTimeout = TUnitSettings.Timeouts.ForcefulExitTimeout;
        _savedProcessExitHookDelay = TUnitSettings.Timeouts.ProcessExitHookDelay;
        _savedMaximumParallelTests = TUnitSettings.Parallelism.MaximumParallelTests;
        _savedDetailedStackTrace = TUnitSettings.Display.DetailedStackTrace;
        _savedFailFast = TUnitSettings.Execution.FailFast;
    }

    [After(HookType.Test)]
    public void RestoreSettings()
    {
        TUnitSettings.Timeouts.DefaultTestTimeout = _savedTestTimeout;
        TUnitSettings.Timeouts.DefaultHookTimeout = _savedHookTimeout;
        TUnitSettings.Timeouts.ForcefulExitTimeout = _savedForcefulExitTimeout;
        TUnitSettings.Timeouts.ProcessExitHookDelay = _savedProcessExitHookDelay;
        TUnitSettings.Parallelism.MaximumParallelTests = _savedMaximumParallelTests;
        TUnitSettings.Display.DetailedStackTrace = _savedDetailedStackTrace;
        TUnitSettings.Execution.FailFast = _savedFailFast;
    }

    [Test]
    public async Task Defaults_Are_Correct()
    {
        await Assert.That(TUnitSettings.Timeouts.DefaultTestTimeout).IsEqualTo(TimeSpan.FromMinutes(30));
        await Assert.That(TUnitSettings.Timeouts.DefaultHookTimeout).IsEqualTo(TimeSpan.FromMinutes(5));
        await Assert.That(TUnitSettings.Timeouts.ForcefulExitTimeout).IsEqualTo(TimeSpan.FromSeconds(30));
        await Assert.That(TUnitSettings.Timeouts.ProcessExitHookDelay).IsEqualTo(TimeSpan.FromMilliseconds(500));
        await Assert.That(TUnitSettings.Parallelism.MaximumParallelTests).IsNull();
        await Assert.That(TUnitSettings.Display.DetailedStackTrace).IsFalse();
        await Assert.That(TUnitSettings.Execution.FailFast).IsFalse();
    }

    [Test]
    public async Task Settings_Can_Be_Modified()
    {
        TUnitSettings.Timeouts.DefaultTestTimeout = TimeSpan.FromMinutes(10);
        await Assert.That(TUnitSettings.Timeouts.DefaultTestTimeout).IsEqualTo(TimeSpan.FromMinutes(10));
    }
}
