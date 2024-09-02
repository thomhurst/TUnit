using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class BeforeTestContext
{
    private readonly DiscoveredTest _discoveredTest;

    internal BeforeTestContext(DiscoveredTest discoveredTest)
    {
        _discoveredTest = discoveredTest;
    }

    public TestContext TestContext => _discoveredTest.TestContext;
    public TestDetails TestDetails => TestContext.TestDetails;

    public void SetTestExecutor(ITestExecutor testExecutor)
    {
        _discoveredTest.TestExecutor = testExecutor;
    }
    
    public void SetHookExecutor(IHookExecutor hookExecutor)
    {
        _discoveredTest.HookExecutor = hookExecutor;
    }
}