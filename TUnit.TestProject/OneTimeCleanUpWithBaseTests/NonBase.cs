using TUnit.Core;

namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class NonBase : Base1
{
    [OnlyOnceCleanUp]
    public static Task NonBaseOneTimeCleanUp()
    {
        return Task.CompletedTask;
    }
    
    [CleanUp]
    public Task NonBaseCleanUp()
    {
        return Task.CompletedTask;
    }

    [Test]
    public void Test()
    {
    }
}