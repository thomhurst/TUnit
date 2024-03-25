using TUnit.Core;

namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class NonBase : Base1
{
    [OnlyOnceSetUp]
    public static Task NonBaseOneTimeSetup()
    {
        return Task.CompletedTask;
    }

    [Test]
    public void Test()
    {
    }
}