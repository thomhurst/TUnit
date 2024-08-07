using TUnit.Core;

namespace TUnit.TestProject.OneTimeCleanUpWithBaseTests;

public class Base1 : Base2
{
    [After(Class)]
    public static Task Base1AfterAllTestsInClass()
    {
        return Task.CompletedTask;
    }
    
    [After(EachTest)]
    public Task Base1CleanUp()
    {
        return Task.CompletedTask;
    }
}