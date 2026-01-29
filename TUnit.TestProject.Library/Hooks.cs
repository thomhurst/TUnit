namespace TUnit.TestProject.Library;

public class Hooks
{
    [Before(Test)]
    public void BeforeTests(TestContext testContext)
    {
        testContext.StateBag.Items["BeforeHit"] = true;
    }

    [After(Test)]
    public void AfterTests(TestContext testContext)
    {
        testContext.StateBag.Items["AfterHit"] = true;
    }
}

/// <summary>
/// Test session hook in a library project.
/// This tests the fix for https://github.com/thomhurst/TUnit/issues/4583
/// where hooks in referenced library projects don't execute.
/// </summary>
public static class LibraryTestSessionHooks
{
    public static bool BeforeTestSessionWasExecuted { get; private set; }
    public static bool AfterTestSessionWasExecuted { get; private set; }

    [Before(TestSession)]
    public static Task BeforeTestSession()
    {
        BeforeTestSessionWasExecuted = true;
        return Task.CompletedTask;
    }

    [After(TestSession)]
    public static Task AfterTestSession()
    {
        AfterTestSessionWasExecuted = true;
        return Task.CompletedTask;
    }
}
