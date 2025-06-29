namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class Base2
{
    [Before(Class)]
    public static Task Base2OneTimeSetup()
    {
        return Task.CompletedTask;
    }

    [Before(Test)]
    public Task Base2SetUp()
    {
        return Task.CompletedTask;
    }
}
