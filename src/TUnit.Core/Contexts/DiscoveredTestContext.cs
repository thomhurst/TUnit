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

    public TestDetails TestDetails => TestContext.Metadata.TestDetails;

    public DiscoveredTestContext(string testName, TestContext testContext)
    {
        TestName = testName;
        TestContext = testContext;
    }

    public void AddCategory(string category)
    {
        if (!string.IsNullOrWhiteSpace(category) && !TestContext.Metadata.TestDetails.Categories.Contains(category))
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
        TestContext.Metadata.TestDetails.RetryLimit = retryCount;
    }

    /// <summary>
    /// Sets the backoff configuration for retry attempts.
    /// </summary>
    /// <param name="backoffMs">Initial delay in milliseconds before the first retry. 0 means no delay.</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff (e.g. 2.0 doubles the delay each retry).</param>
    public void SetRetryBackoff(int backoffMs, double backoffMultiplier)
    {
        TestContext.Metadata.TestDetails.RetryBackoffMs = backoffMs;
        TestContext.Metadata.TestDetails.RetryBackoffMultiplier = backoffMultiplier;
    }

    /// <summary>
    /// Adds a parallel constraint to the test context.
    /// Multiple constraints can be combined (e.g., ParallelGroup + NotInParallel).
    /// </summary>
    public void AddParallelConstraint(IParallelConstraint constraint)
    {
        TestContext.Parallelism.AddConstraint(constraint);
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
