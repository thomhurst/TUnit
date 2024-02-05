using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class CustomSkipAttribute : Attribute, ITestAttribute
{
    public Task ApplyToTest(TestContext testContext)
    {
        testContext.SkipTest("Blah!");
        return Task.CompletedTask;
    }
}