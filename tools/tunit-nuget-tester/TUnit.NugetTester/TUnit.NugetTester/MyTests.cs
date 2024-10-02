using TUnit.NugetTester.Library;
using TUnit.Core.Logging;

namespace TUnit.NugetTester;

public class MyTests : TestBase
{
    [Test]
    public void Test()
    {
        TestContext.Current!.GetDefaultLogger().LogInformation("Blah");
    }
}