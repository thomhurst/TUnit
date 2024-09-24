namespace TUnit.NugetTester.Library;

public class TestBase
{
    [Before(Test)]
    public void Setup(TestContext testContext)
    {
        Console.WriteLine($"Starting test: {testContext.TestDetails.DisplayName}");
    }
    
    [After(Test)]
    public void Teardown(TestContext testContext)
    {
        Console.WriteLine($"Finishing test: {testContext.TestDetails.DisplayName}");
    }
}