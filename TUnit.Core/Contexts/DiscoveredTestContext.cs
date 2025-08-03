using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for test discovery phase - builds up test configuration
/// </summary>
public class DiscoveredTestContext
{
    private readonly List<string> _categories =
    [
    ];
    private readonly List<Func<object?, string?>> _argumentDisplayFormatters =
    [
    ];

    // Typed fields for known configuration
    private Type? _displayNameFormatter;
    private Func<TestContext, Exception, int, Task<bool>>? _shouldRetryFunc;
    private IParallelConstraint? _parallelConstraint;
    private Priority _priority = Priority.Normal;

    public string TestName { get; }
    public TestContext TestContext
    {
        get;
    }

    public TestDetails TestDetails => TestContext.TestDetails;
    public bool RunOnTestDiscovery { get; private set; }

    public DiscoveredTestContext(string testName, TestContext testContext)
    {
        TestName = testName;
        TestContext = testContext;
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
            values =
            [];
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
        TestContext.CustomDisplayName = displayName;
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

    public void SetPriority(Priority priority)
    {
        _priority = priority;
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
        testContext.RetryFunc = _shouldRetryFunc;
        testContext.ParallelConstraint = _parallelConstraint;
        testContext.ExecutionPriority = _priority;
        testContext.RunOnTestDiscovery = RunOnTestDiscovery;

        foreach (var formatter in _argumentDisplayFormatters)
        {
            testContext.ArgumentDisplayFormatters.Add(formatter);
        }
    }

    /// <summary>
    /// Gets the test display name
    /// </summary>
    public string GetDisplayName()
    {
        return TestContext.GetDisplayName();
    }
}
