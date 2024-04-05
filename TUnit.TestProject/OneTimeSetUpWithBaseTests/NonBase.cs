using TUnit.Core;

namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class NonBase : Base1
{
    [OneTimeSetUp]
    public static Task NonBaseOneTimeSetup()
    {
        return Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public Task NonBaseSetUp()
    {
        return Task.CompletedTask;
    }

    [Test]
    public void Test()
    {
    }
}