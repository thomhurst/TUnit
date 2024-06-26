using TUnit.Core;

namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class Base2
{
    [AfterAllTestsInClass]
    public static Task Base2AfterAllTestsInClass()
    {
        return Task.CompletedTask;
    }
    
    [AfterEachTest]
    public Task Base2CleanUp()
    {
        return Task.CompletedTask;
    }
}