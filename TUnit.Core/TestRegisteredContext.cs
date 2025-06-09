using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents the context for a registered test.
/// </summary>
public class TestRegisteredContext : BeforeTestContext
{
    internal TestRegisteredContext(DiscoveredTest discoveredTest) : base(discoveredTest)
    {
    }

    /// <summary>
    /// Sets the parallel limiter for the test.
    /// </summary>
    /// <param name="parallelLimit">The parallel limit.</param>
    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        DiscoveredTest.TestDetails.ParallelLimit = parallelLimit;
    }

    /// <summary>
    /// Skips the test with the specified reason.
    /// </summary>
    /// <param name="reason">The reason for skipping the test.</param>
    public void SkipTest(string reason)
    {
        DiscoveredTest.TestContext.SkipReason = reason;
    }

    public void SetTaskScheduler(TaskScheduler taskScheduler)
    {
        DiscoveredTest.TestContext.TaskScheduler = taskScheduler;
    }
}
