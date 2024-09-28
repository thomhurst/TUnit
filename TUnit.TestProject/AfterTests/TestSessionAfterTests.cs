namespace TUnit.TestProject.AfterTests;

public class TestSessionAfterTests
{
    [After(TestSession)]
    public static async Task AfterTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
    }

    [AfterEvery(TestSession)]
    public static async Task AfterEveryTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
    }
}
