using TUnit.Core;

namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class Base1 : Base2
{
    [AfterAllTestsInClass]
    public static Task Base1AfterAllTestsInClass()
    {
        return Task.CompletedTask;
    }
    
    [AfterEachTest]
    public Task Base1CleanUp()
    {
        return Task.CompletedTask;
    }
}