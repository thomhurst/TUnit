using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._7289;

/// <summary>
/// Regression for issue #7289 (PR #7309 review): when the test body passes but an [After(Test)]
/// hook throws, late test-end receivers must see the Failed result that will be reported.
/// TUnit.Playwright's [CaptureOnFailure] decides at that point whether to keep screenshots and
/// traces. This test fails on purpose; the engine test asserts what the receiver observed.
/// </summary>
public class LateReceiverSeesTeardownFailure
{
    [Test]
    [RecordObservedResult]
    public void Body_Passes_But_Teardown_Throws()
    {
    }

    [After(Test)]
    public void FailingTeardown() => throw new InvalidOperationException("Teardown failed on purpose");

    public sealed class RecordObservedResultAttribute : Attribute, ITestEndEventReceiver
    {
        public int Order => 0;

        public async ValueTask OnTestEnd(TestContext context)
        {
            // The engine test passes a run id, so it only asserts files written by its own run.
            var runId = Environment.GetEnvironmentVariable("TUNIT_7289_RUN_ID") ?? Guid.NewGuid().ToString("N");
            var state = context.Execution.Result?.State.ToString() ?? "None";
            await File.WriteAllTextAsync($"{runId}-LateReceiverSeesTeardownFailure-{state}.txt", state);
        }
    }
}
