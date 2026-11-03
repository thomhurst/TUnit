using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class SynchronousTimeoutTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    [Timeout(200)]
    [EngineTest(ExpectedResult.Failure)]
    public async Task Blocking_Body_Times_Out(bool yieldBeforeBlocking)
    {
        if (yieldBeforeBlocking)
        {
            await Task.Yield();
        }

        // Bound the wait so a regression fails the duration assertion rather than hanging CI.
        using var blocker = new ManualResetEventSlim();
        blocker.Wait(TimeSpan.FromSeconds(10));
    }
}
