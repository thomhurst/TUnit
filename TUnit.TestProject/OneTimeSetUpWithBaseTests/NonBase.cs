using TUnit.Core;

namespace TUnit.TestProject.BeforeAllTestsInClassWithBaseTests;

public class NonBase : Base1
{
    [BeforeAllTestsInClass]
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