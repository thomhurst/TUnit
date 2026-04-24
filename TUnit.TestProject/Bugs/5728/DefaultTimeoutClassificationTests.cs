using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5728;

/// <summary>
/// Reproduction for PR #5728 Bug 2: timeout failures triggered by
/// <c>TimeoutSettings.DefaultTestTimeout</c> were misclassified as generic errors
/// instead of timeouts because <c>TestDetails.Timeout</c> was left null when the timeout
/// was resolved from settings rather than a <c>[Timeout]</c> attribute.
///
/// Only the targeted engine-test filter runs this class — its <c>EngineTest=Failure</c>
/// marker excludes it from the default pass-only harness run.
/// </summary>
public class DefaultTimeoutClassificationHooks
{
    // Gated on TUNIT_BUG5728_DEFAULT_TIMEOUT so this process-wide setting only takes effect
    // when the engine-test harness explicitly opts in — otherwise discovery hooks fire for
    // every run of TUnit.TestProject and would shrink every test's default timeout to 200ms.
    //
    // Method name must contain "Before" for the source generator to match it against the
    // BeforeTestDiscoveryContext parameter (see HookMetadataGenerator.IsValidHookMethod).
    [Before(TestDiscovery)]
    public static void BeforeDiscovery_ConfigureDefaultTimeoutWhenTargeted(BeforeTestDiscoveryContext context)
    {
        if (Environment.GetEnvironmentVariable("TUNIT_BUG5728_DEFAULT_TIMEOUT") != "1")
        {
            return;
        }

        context.Settings.Timeouts.DefaultTestTimeout = TimeSpan.FromMilliseconds(200);
    }
}

public class DefaultTimeoutClassificationTests
{
    /// <summary>
    /// No <c>[Timeout]</c> attribute — timeout is inherited from <c>DefaultTestTimeout</c>.
    /// Uses a non-cancellable delay so the timeout path (not cooperative cancellation)
    /// fires deterministically, matching the scenario that originally misclassified.
    /// </summary>
    [Test]
    [EngineTest(ExpectedResult.Failure)]
    public async Task Hanging_Test_With_DefaultTestTimeout_Should_Timeout()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
    }
}
