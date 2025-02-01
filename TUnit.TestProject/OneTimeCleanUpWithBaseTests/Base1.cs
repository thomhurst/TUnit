namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class Base1 : Base2
{
    [After(Class)]
    public static Task Base1AfterAllTestsInClass()
    {
        return Task.CompletedTask;
    }

    [After(Test)]
    public Task Base1CleanUp()
    {
        return Task.CompletedTask;
    }
}