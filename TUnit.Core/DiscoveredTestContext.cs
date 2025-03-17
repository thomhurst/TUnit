using TUnit.Core.Interfaces;

#pragma warning disable CS9113 // Parameter is unread.

namespace TUnit.Core;

/// <summary>
/// Represents the context for a discovered test.
/// </summary>
public class DiscoveredTestContext
{
    public TestContext TestContext { get; }
    public TestDetails TestDetails => TestContext.TestDetails;

    internal DiscoveredTestContext(TestContext testContext)
    {
        TestContext = testContext;
    }

    public void AddProperty(string key, string value)
    {
        TestContext.TestDetails.InternalCustomProperties.Add(key, value);
    }
    
    public void AddCategory(string category)
    {
        TestContext.TestDetails.MutableCategories.Add(category);
    }
    
    public void SetDisplayName(string displayName)
    {
        TestContext.TestDetails.DisplayName = displayName;
    }
    
    public void AddArgumentDisplayFormatter(ArgumentDisplayFormatter formatter)
    {
        TestContext.ArgumentDisplayFormatters.Add(formatter);
    }

    public void SetParallelConstraint(IParallelConstraint parallelConstraint)
    {
        TestContext.TestDetails.ParallelConstraint = parallelConstraint;
    }
    
    public void SetRetryCount(int times)
    {
        SetRetryCount(times, (_, _, _) => Task.FromResult(true));
    }
    
    public void SetRetryCount(int times, Func<TestContext, Exception, int, Task<bool>> shouldRetry)
    {
        TestContext.TestDetails.RetryLimit = times;
        TestContext.TestDetails.RetryLogic = shouldRetry;
    }
}