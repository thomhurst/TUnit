using TUnit.Core;

namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class Base1 : Base2
{
    [OneTimeCleanUp]
    public static Task Base1OneTimeCleanUp()
    {
        return Task.CompletedTask;
    }
    
    [CleanUp]
    public Task Base1CleanUp()
    {
        return Task.CompletedTask;
    }
}