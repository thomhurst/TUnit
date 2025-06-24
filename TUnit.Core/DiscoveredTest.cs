using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents a discovered test
/// </summary>
public abstract class DiscoveredTest
{
    public required TestContext TestContext { get; init; }
    public TestDetails TestDetails => TestContext.TestDetails;
    public ITestExecutor? TestExecutor { get; set; }
}

/// <summary>
/// Generic version of DiscoveredTest
/// </summary>
public class DiscoveredTest<T> : DiscoveredTest where T : class
{
}