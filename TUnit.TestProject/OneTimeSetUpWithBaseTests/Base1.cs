using TUnit.Core;

namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class Base1 : Base2
{
    [OneTimeSetUp]
    public static Task Base1OneTimeSetup()
    {
        return Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public Task Base1SetUp()
    {
        return Task.CompletedTask;
    }
}