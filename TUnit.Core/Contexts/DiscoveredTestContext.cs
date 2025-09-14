using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for test discovery phase - builds up test configuration
/// </summary>
public class DiscoveredTestContext
{
    public string TestName { get; }
    public TestContext TestContext { get; }

    public TestDetails TestDetails => TestContext.TestDetails;

    public DiscoveredTestContext(string testName, TestContext testContext)
    {
        TestName = testName;
        TestContext = testContext;
    }

    public void AddCategory(string category)
    {
        if (!string.IsNullOrWhiteSpace(category) && !TestContext.TestDetails.Categories.Contains(category))
        {
            TestDetails.Categories.Add(category);
        }
    }

    public void AddProperty(string key, string value)
    {
        if (!TestDetails.CustomProperties.TryGetValue(key, out var values))
        {
            values = [];
            TestDetails.CustomProperties[key] = values;
        }

        values.Add(value);
    }


    public void SetDisplayNameFormatter(Type formatterType)
    {
        TestContext.DisplayNameFormatter = formatterType;
    }

    public void SetDisplayName(string displayName)
    {
        TestContext.CustomDisplayName = displayName;
    }

    public void SetRetryLimit(int retryLimit)
    {
        SetRetryLimit(retryLimit, (_, _, _) => Task.FromResult(true));
    }

    public void SetRetryLimit(int retryCount, Func<TestContext, Exception, int, Task<bool>> shouldRetry)
    {
        TestContext.RetryFunc = shouldRetry;
        TestContext.TestDetails.RetryLimit = retryCount;
    }

    public void SetParallelConstraint(IParallelConstraint constraint)
    {
        TestContext.ParallelConstraint = constraint;
    }

    public void AddArgumentDisplayFormatter(ArgumentDisplayFormatter formatter)
    {
        TestContext.ArgumentDisplayFormatters.Add(obj => formatter.CanHandle(obj) ? formatter.FormatValue(obj) : null);
    }

    public void SetPriority(Priority priority)
    {
        TestContext.ExecutionPriority = priority;
    }


    /// <summary>
    /// Gets the argument display formatters
    /// </summary>
    public List<Func<object?, string?>> ArgumentDisplayFormatters => TestContext.ArgumentDisplayFormatters;

    /// <summary>
    /// Gets the test display name
    /// </summary>
    public string GetDisplayName()
    {
        return TestContext.GetDisplayName();
    }
}
