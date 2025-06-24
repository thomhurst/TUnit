using TUnit.Core.Interfaces;

namespace TUnit.Core;

// Legacy compatibility types - These exist to support attributes that haven't been updated yet.

public class DiscoveredTestContext : TestContext
{
    private readonly List<string> _categories = new();
    
    public DiscoveredTestContext(string testName, string displayName) : base(testName, displayName)
    {
    }
    
    public void AddCategory(string category)
    {
        if (!string.IsNullOrWhiteSpace(category) && !_categories.Contains(category))
        {
            _categories.Add(category);
            if (TestDetails != null && !TestDetails.Categories.Contains(category))
            {
                TestDetails.Categories.Add(category);
            }
        }
    }
    
    public void AddProperty(string key, string value)
    {
        if (TestDetails != null)
        {
            if (!TestDetails.CustomProperties.TryGetValue(key, out var values))
            {
                values = new List<string>();
                TestDetails.CustomProperties[key] = values;
            }
            values.Add(value);
        }
    }
    
    public void SetProperty(string key, string value)
    {
        Items[key] = value;
    }
    
    public void SetDisplayNameFormatter(Type formatterType)
    {
        Items["DisplayNameFormatter"] = formatterType;
    }
    
    public void SetDisplayName(string displayName)
    {
        if (TestDetails != null)
        {
            TestDetails.DisplayName = displayName;
        }
    }
    
    public void SetRetryLimit(int retryLimit)
    {
        if (TestDetails != null)
        {
            TestDetails.RetryLimit = retryLimit;
        }
    }
    
    public void SetRetryCount(int retryCount, Func<TestContext, Exception, int, Task<bool>> shouldRetry)
    {
        SetRetryLimit(retryCount);
        Items["ShouldRetryFunc"] = shouldRetry;
    }
    
    public void SetParallelConstraint(object constraint)
    {
        Items["ParallelConstraint"] = constraint;
    }
    
    public void AddArgumentDisplayFormatter(ArgumentDisplayFormatter formatter)
    {
        ArgumentDisplayFormatters.Add(obj => formatter.CanHandle(obj) ? formatter.FormatValue(obj) : null);
    }
    
    public void SetRunOnDiscovery(bool runOnDiscovery)
    {
        RunOnTestDiscovery = runOnDiscovery;
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