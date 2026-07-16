namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class NonBase : Base1
{
    [After(Class)]
    public static Task NonBaseAfterAllTestsInClass()
    {
        return Task.CompletedTask;
    }

    [After(HookType.Test)]
    public Task NonBaseCleanUp()
    {
        return Task.CompletedTask;
    }

    [Test]
    public void Test()
    {
        // Dummy method
    }
}
