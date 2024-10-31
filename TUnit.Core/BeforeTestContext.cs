using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class TestRegisteredContext : BeforeTestContext
{
    internal TestRegisteredContext(DiscoveredTest discoveredTest) : base(discoveredTest)
    {
    }

    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        DiscoveredTest.TestDetails.ParallelLimit = parallelLimit;
    }

    public void SkipTest(string reason)
    {
        DiscoveredTest.TestContext.SkipReason = reason;
    }
}

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
}