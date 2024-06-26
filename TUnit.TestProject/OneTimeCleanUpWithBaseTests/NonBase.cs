using TUnit.Core;

namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class NonBase : Base1
{
    [AfterAllTestsInClass]
    public static Task NonBaseAfterAllTestsInClass()
    {
        return Task.CompletedTask;
    }
    
    [AfterEachTest]
    public Task NonBaseCleanUp()
    {
        return Task.CompletedTask;
    }

    [Test]
    public void Test()
    {
    }
}