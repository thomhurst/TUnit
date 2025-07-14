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
        TestName = testContext.TestName;
        CustomDisplayName = testContext.CustomDisplayName;
    }

    /// <summary>
    /// Gets the object bag from the underlying TestContext
    /// </summary>
    public Dictionary<string, object?> ObjectBag => TestContext.ObjectBag;

    /// <summary>
    /// Gets the test details from the underlying TestContext
    /// </summary>
    public TestDetails TestDetails => TestContext.TestDetails;

    public void SetTestExecutor(ITestExecutor executor)
    {
        DiscoveredTest.TestExecutor = executor;
    }

    /// <summary>
    /// Sets the parallel limiter for the test
    /// </summary>
    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        TestContext.SetParallelLimiter(parallelLimit);
    }
}
