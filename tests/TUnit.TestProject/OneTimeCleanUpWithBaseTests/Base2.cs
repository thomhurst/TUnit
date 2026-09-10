namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class Base2
{
    [After(Class)]
    public static Task Base2AfterAllTestsInClass()
    {
        return Task.CompletedTask;
    }

    [After(Test)]
    public Task Base2CleanUp()
    {
        return Task.CompletedTask;
    }
}
