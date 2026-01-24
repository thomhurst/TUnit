using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.Issue4541;

/// <summary>
/// Reproduction test for https://github.com/thomhurst/TUnit/issues/4541
/// Tests that [Before(TestSession)] hooks on abstract base classes are executed.
/// </summary>
public abstract class AbstractBaseWithTestSessionHook
{
    public static bool TestSessionHookWasExecuted { get; private set; }

    [Before(TestSession)]
    public static Task BeforeTestSession()
    {
        TestSessionHookWasExecuted = true;
        return Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ConcreteTestClass : AbstractBaseWithTestSessionHook
{
    [Test]
    public async Task Verify_TestSession_Hook_On_Abstract_Base_Class_Executed()
    {
        await Assert.That(TestSessionHookWasExecuted).IsTrue()
            .Because("[Before(TestSession)] hook on abstract base class should have executed");
    }
}
