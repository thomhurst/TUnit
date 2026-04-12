using TUnit.Core.Settings;

namespace TUnit.UnitTests;

[NotInParallel]
public class TUnitSettingsTests
{
    [Test]
    public async Task Defaults_Are_Correct()
    {
        await Assert.That(TUnitSettings.Timeouts.DefaultTestTimeout).IsEqualTo(TimeSpan.FromMinutes(30));
        await Assert.That(TUnitSettings.Timeouts.DefaultHookTimeout).IsEqualTo(TimeSpan.FromMinutes(5));
        await Assert.That(TUnitSettings.Timeouts.ForcefulExitTimeout).IsEqualTo(TimeSpan.FromSeconds(30));
        await Assert.That(TUnitSettings.Timeouts.ProcessExitHookDelay).IsEqualTo(TimeSpan.FromMilliseconds(500));
        await Assert.That(TUnitSettings.Parallelism.MaximumParallelTests).IsNull();
        await Assert.That(TUnitSettings.Display.DisableLogo).IsFalse();
        await Assert.That(TUnitSettings.Display.DetailedStackTrace).IsFalse();
        await Assert.That(TUnitSettings.Execution.FailFast).IsFalse();
    }

    [Test]
    public async Task Settings_Can_Be_Modified()
    {
        var originalTimeout = TUnitSettings.Timeouts.DefaultTestTimeout;

        try
        {
            TUnitSettings.Timeouts.DefaultTestTimeout = TimeSpan.FromMinutes(10);
            await Assert.That(TUnitSettings.Timeouts.DefaultTestTimeout).IsEqualTo(TimeSpan.FromMinutes(10));
        }
        finally
        {
            TUnitSettings.Timeouts.DefaultTestTimeout = originalTimeout;
        }
    }
}
