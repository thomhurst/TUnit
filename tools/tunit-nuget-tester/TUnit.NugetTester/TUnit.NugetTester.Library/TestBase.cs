using TUnit.Core.Extensions;

namespace TUnit.NugetTester.Library;

public class TestBase
{
    [Before(Test)]
    public void Setup(TestContext testContext)
    {
        Console.WriteLine($"Starting test: {testContext.TestDetails.GetTestDisplayName()}");
    }
    
    [After(Test)]
    public void Teardown(TestContext testContext)
    {
        Console.WriteLine($"Finishing test: {testContext.TestDetails.GetTestDisplayName()}");
    }
}