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