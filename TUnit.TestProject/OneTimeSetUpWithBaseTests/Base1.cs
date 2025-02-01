namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class Base1 : Base2
{
    [Before(Class)]
    public static Task Base1OneTimeSetup()
    {
        return Task.CompletedTask;
    }

    [Before(Test)]
    public Task Base1SetUp()
    {
        return Task.CompletedTask;
    }
}