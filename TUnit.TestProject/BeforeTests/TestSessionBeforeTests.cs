namespace TUnit.TestProject.BeforeTests;

public class TestSessionBeforeTests
{
    [Before(TestSession)]
    public static async Task BeforeTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
    }

    [BeforeEvery(TestSession)]
    public static async Task BeforeEveryTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
    }
}
