using TUnit.NugetTester.Library;

namespace TUnit.NugetTester;

public class MyTests : TestBase
{
    [Test]
    public void Test()
    {
        TestContext.Current!.GetDefaultLogger().LogInformation("Blah");
    }
}