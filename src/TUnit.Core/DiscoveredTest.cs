using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class DiscoveredTest
{
    public required TestContext TestContext { get; init; }
    public TestDetails TestDetails => TestContext.Metadata.TestDetails;
    public ITestExecutor? TestExecutor { get; set; }
}

public class DiscoveredTest<T> : DiscoveredTest where T : class;
