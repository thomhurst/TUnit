using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for test discovery phase
/// </summary>
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
            if (!TestDetails.Categories.Contains(category))
            {
                TestDetails.Categories.Add(category);
            }
        }
    }
    
    public void AddProperty(string key, string value)
    {
        if (!TestDetails.CustomProperties.TryGetValue(key, out var values))
        {
            values = new List<string>();
            TestDetails.CustomProperties[key] = values;
        }
        values.Add(value);
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
        TestDetails.DisplayName = displayName;
    }
    
    public void SetRetryLimit(int retryLimit)
    {
        TestDetails.RetryLimit = retryLimit;
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