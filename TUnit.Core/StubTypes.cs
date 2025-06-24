using TUnit.Core.Interfaces;

namespace TUnit.Core;

// Legacy compatibility types - These exist to support attributes that haven't been updated yet.
// TODO: Update attributes to use TestContext directly and remove these types.

public class DiscoveredTestContext : TestContext
{
    public DiscoveredTestContext(string testName, string displayName) : base(testName, displayName)
    {
    }
    
    // All methods are now inherited from TestContext
    
    public void SetProperty(string key, string value)
    {
        Items[key] = value;
    }
    
    public void SetDisplayNameFormatter(Type formatterType)
    {
        Items["DisplayNameFormatter"] = formatterType;
    }
}

public class BeforeTestContext : TestContext
{
    public BeforeTestContext(string testName, string displayName) : base(testName, displayName)
    {
    }
}

public class AfterTestContext : TestContext
{
    public AfterTestContext(string testName, string displayName) : base(testName, displayName)
    {
    }
}

public abstract class DiscoveredTest
{
    public required TestContext TestContext { get; init; }
    public TestDetails TestDetails => TestContext.TestDetails!;
    public ITestExecutor? TestExecutor { get; set; }
}

public class TestRegisteredContext : BeforeTestContext
{
    public TestRegisteredContext(string testName, string displayName) : base(testName, displayName)
    {
    }
    
    public DiscoveredTest DiscoveredTest { get; set; } = null!;
    
    public void SetTestExecutor(ITestExecutor executor)
    {
        if (InternalDiscoveredTest != null)
        {
            InternalDiscoveredTest.TestExecutor = executor;
        }
    }
    
    // SetParallelLimiter is now inherited from TestContext
}

// Generic version for compatibility
public class DiscoveredTest<T> : DiscoveredTest where T : class
{
}

public interface ITestDefinition
{
}

public class TestDefinition : ITestDefinition
{
}

public class TestDefinition<T> : TestDefinition where T : class
{
}

public class DiscoveryResult
{
    public static DiscoveryResult Empty => new DiscoveryResult();
}

public abstract class DynamicTest
{
    public abstract IEnumerable<DiscoveryResult> GetTests();
}

public abstract class DynamicTest<T> : DynamicTest where T : class
{
}

public interface IDynamicTestSource
{
    IEnumerable<DynamicTest> GetTests();
}