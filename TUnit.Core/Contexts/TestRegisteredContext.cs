using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for test registration phase
/// </summary>
public class TestRegisteredContext
{
    public string TestName { get; }
    public string? CustomDisplayName { get; }
    public TestContext TestContext { get; }
    public DiscoveredTest DiscoveredTest { get; set; } = null!;

    public TestRegisteredContext(TestContext testContext)
    {
        TestContext = testContext;
        TestName = testContext.Metadata.TestDetails.TestName;
        CustomDisplayName = testContext.CustomDisplayName;
    }

    /// <summary>
    /// Gets the object bag from the underlying TestContext
    /// </summary>
    public ConcurrentDictionary<string, object?> ObjectBag => TestContext.StateBag.Items;

    /// <summary>
    /// Gets the test details from the underlying TestContext
    /// </summary>
    public TestDetails TestDetails => TestContext.Metadata.TestDetails;

    public void SetTestExecutor(ITestExecutor executor)
    {
        DiscoveredTest.TestExecutor = executor;
    }

    /// <summary>
    /// Sets a custom hook executor that will be used for all test-level hooks (Before/After Test).
    /// This allows you to wrap hook execution in custom logic (e.g., running on a specific thread).
    /// </summary>
    public void SetHookExecutor(IHookExecutor executor)
    {
        TestContext.CustomHookExecutor = executor;
    }

    /// <summary>
    /// Sets the parallel limiter for the test
    /// </summary>
    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        TestContext.ParallelLimiter = parallelLimit;
    }

    /// <summary>
    /// Marks the test as skipped with the specified reason.
    /// This can only be called during the test registration phase.
    /// </summary>
    /// <param name="reason">The reason why the test is being skipped</param>
    public void SetSkipped(string reason)
    {
        TestContext.SkipReason = reason;
        TestContext.Metadata.TestDetails.ClassInstance = SkippedTestInstance.Instance;
    }
}
