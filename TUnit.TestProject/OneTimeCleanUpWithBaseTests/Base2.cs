using TUnit.Core;

namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class Base2
{
    [OneTimeCleanUp]
    public static Task Base2OneTimeCleanUp()
    {
        return Task.CompletedTask;
    }
    
    [AfterEachTest]
    public Task Base2CleanUp()
    {
        return Task.CompletedTask;
    }
}