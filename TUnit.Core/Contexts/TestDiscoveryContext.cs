using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TUnit.Core.Contexts;

/// <summary>
/// Context used during test discovery phase when attributes can modify test metadata
/// </summary>
public sealed class TestDiscoveryContext : ContextBase
{
    private readonly List<string> _categories = new();
    private readonly Dictionary<string, List<string>> _customProperties = new();
    
    /// <summary>
    /// The test being discovered
    /// </summary>
    public TestDetails TestDetails { get; }
    
    /// <summary>
    /// Categories assigned to this test
    /// </summary>
    public IReadOnlyList<string> Categories => _categories;
    
    /// <summary>
    /// Custom properties assigned to this test
    /// </summary>
    public IReadOnlyDictionary<string, List<string>> CustomProperties => _customProperties;
    
    /// <summary>
    /// Argument display formatters for the test
    /// </summary>
    public List<Func<object?, string?>> ArgumentDisplayFormatters { get; } = new();
    
    /// <summary>
    /// Whether to run this test during discovery
    /// </summary>
    public bool RunOnTestDiscovery { get; set; }
    
    public TestDiscoveryContext(TestDetails testDetails)
    {
        TestDetails = testDetails ?? throw new ArgumentNullException(nameof(testDetails));
    }
    
    /// <summary>
    /// Adds a category to the test
    /// </summary>
    public void AddCategory(string category)
    {
        if (!string.IsNullOrWhiteSpace(category) && !_categories.Contains(category))
        {
            _categories.Add(category);
            TestDetails.Categories.Add(category);
        }
    }
    
    /// <summary>
    /// Adds a custom property to the test
    /// </summary>
    public void AddProperty(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Property key cannot be null or empty", nameof(key));
            
        if (!_customProperties.TryGetValue(key, out var values))
        {
            values = new List<string>();
            _customProperties[key] = values;
        }
        values.Add(value);
        
        // Update TestDetails
        if (!TestDetails.CustomProperties.TryGetValue(key, out var detailValues))
        {
            detailValues = new List<string>();
            TestDetails.CustomProperties[key] = detailValues;
        }
        detailValues.Add(value);
    }
    
    /// <summary>
    /// Sets the display name for the test
    /// </summary>
    public void SetDisplayName(string displayName)
    {
        TestDetails.DisplayName = displayName;
    }
    
    /// <summary>
    /// Sets the retry limit for the test
    /// </summary>
    public void SetRetryLimit(int retryLimit)
    {
        if (retryLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(retryLimit), "Retry limit must be non-negative");
            
        TestDetails.RetryLimit = retryLimit;
    }
    
    /// <summary>
    /// Sets the retry count with a custom retry function
    /// </summary>
    public void SetRetryCount(int retryCount, Func<TestContext, Exception, int, Task<bool>> shouldRetry)
    {
        SetRetryLimit(retryCount);
        Items["ShouldRetryFunc"] = shouldRetry;
    }
    
    /// <summary>
    /// Sets a parallel constraint for the test
    /// </summary>
    public void SetParallelConstraint(object constraint)
    {
        Items["ParallelConstraint"] = constraint ?? throw new ArgumentNullException(nameof(constraint));
    }
    
    /// <summary>
    /// Adds an argument display formatter
    /// </summary>
    public void AddArgumentDisplayFormatter(ArgumentDisplayFormatter formatter)
    {
        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));
            
        ArgumentDisplayFormatters.Add(obj => formatter.CanHandle(obj) ? formatter.FormatValue(obj) : null);
    }
    
    /// <summary>
    /// Sets whether to run on discovery
    /// </summary>
    public void SetRunOnDiscovery(bool runOnDiscovery)
    {
        RunOnTestDiscovery = runOnDiscovery;
    }
}