using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class BeforeTestContext
{
    internal readonly DiscoveredTest DiscoveredTest;

    internal BeforeTestContext(DiscoveredTest discoveredTest)
    {
        DiscoveredTest = discoveredTest;
    }

    public TestContext TestContext => DiscoveredTest.TestContext;
    public TestDetails TestDetails => TestContext.TestDetails;

    public void SetTestExecutor(ITestExecutor testExecutor)
    {
        DiscoveredTest.TestExecutor = testExecutor;
    }
    
    public void SetHookExecutor(IHookExecutor hookExecutor)
    {
        DiscoveredTest.HookExecutor = hookExecutor;
    }
    
    public void AddLinkedCancellationToken(CancellationToken cancellationToken)
    {
        DiscoveredTest.TestContext.LinkedCancellationTokens.Add(cancellationToken);
    }
    
    public void AddAsyncLocalValues() => TestContext.AddAsyncLocalValues();
    
    public static implicit operator TestContext(BeforeTestContext beforeTestContext) => beforeTestContext.TestContext;
}