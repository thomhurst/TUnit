using TUnit.Core.Extensions;

namespace TUnit.NugetTester.Library;

public class TestBase
{
    [Before(Test)]
    public void Setup(TestContext testContext)
    {
        Console.WriteLine($"Starting test: {testContext.GetDisplayName()}");
    }

    [After(Test)]
    public void Teardown(TestContext testContext)
    {
        Console.WriteLine($"Finishing test: {testContext.GetDisplayName()}");
    }
}
