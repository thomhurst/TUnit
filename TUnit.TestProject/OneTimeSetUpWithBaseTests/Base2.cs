using TUnit.Core;

namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class Base2
{
    [OnlyOnceSetUp]
    public static Task Base2OneTimeSetup()
    {
        return Task.CompletedTask;
    }
    
    [SetUp]
    public Task Base2SetUp()
    {
        return Task.CompletedTask;
    }
}