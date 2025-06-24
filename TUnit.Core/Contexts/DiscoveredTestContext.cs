using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for test discovery phase - builds up test configuration
/// </summary>
public class DiscoveredTestContext
{
    private readonly List<string> _categories = new();
    private readonly List<Func<object?, string?>> _argumentDisplayFormatters = new();
    
    // Typed fields for known configuration
    private Type? _displayNameFormatter;
    private Func<TestContext, Exception, int, Task<bool>>? _shouldRetryFunc;
    private IParallelConstraint? _parallelConstraint;
    
    public string TestName { get; }
    public string DisplayName { get; private set; }
    public TestDetails TestDetails { get; }
    public bool RunOnTestDiscovery { get; private set; }
    
    public DiscoveredTestContext(string testName, string displayName, TestDetails testDetails)
    {
        TestName = testName;
        DisplayName = displayName;
        TestDetails = testDetails;
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
    
    
    public void SetDisplayNameFormatter(Type formatterType)
    {
        _displayNameFormatter = formatterType;
    }
    
    public void SetDisplayName(string displayName)
    {
        DisplayName = displayName;
        TestDetails.DisplayName = displayName;
    }
    
    public void SetRetryLimit(int retryLimit)
    {
        TestDetails.RetryLimit = retryLimit;
    }
    
    public void SetRetryCount(int retryCount, Func<TestContext, Exception, int, Task<bool>> shouldRetry)
    {
        SetRetryLimit(retryCount);
        _shouldRetryFunc = shouldRetry;
    }
    
    public void SetParallelConstraint(object constraint)
    {
        _parallelConstraint = constraint as IParallelConstraint;
    }
    
    public void AddArgumentDisplayFormatter(ArgumentDisplayFormatter formatter)
    {
        _argumentDisplayFormatters.Add(obj => formatter.CanHandle(obj) ? formatter.FormatValue(obj) : null);
    }
    
    public void SetRunOnDiscovery(bool runOnDiscovery)
    {
        RunOnTestDiscovery = runOnDiscovery;
    }
    
    
    /// <summary>
    /// Gets the argument display formatters
    /// </summary>
    public List<Func<object?, string?>> ArgumentDisplayFormatters => _argumentDisplayFormatters;
    
    /// <summary>
    /// Transfers configuration to a TestContext
    /// </summary>
    public void TransferTo(TestContext testContext)
    {
        testContext.DisplayNameFormatter = _displayNameFormatter;
        testContext.ShouldRetryFunc = _shouldRetryFunc;
        testContext.ParallelConstraint = _parallelConstraint;
        testContext.RunOnTestDiscovery = RunOnTestDiscovery;
        
        foreach (var formatter in _argumentDisplayFormatters)
        {
            testContext.ArgumentDisplayFormatters.Add(formatter);
        }
    }
}