using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Simplified test context for the new architecture
/// </summary>
public class TestContext
{
    private static readonly AsyncLocal<TestContext?> TestContexts = new();
    internal static readonly Dictionary<string, string> InternalParametersDictionary = new();
    
    private readonly StringWriter _outputWriter = new();
    private readonly StringWriter _errorWriter = new();
    
    /// <summary>
    /// Gets or sets the current test context.
    /// </summary>
    public static TestContext? Current
    {
        get => TestContexts.Value;
        internal set => TestContexts.Value = value;
    }
    
    /// <summary>
    /// Gets the parameters for the test context.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Parameters => InternalParametersDictionary;
    
    /// <summary>
    /// Gets or sets the configuration for the test context.
    /// </summary>
    public static IConfiguration Configuration { get; internal set; } = null!;
    
    /// <summary>
    /// Gets the output directory for the test context.
    /// </summary>
    public static string? OutputDirectory
    {
        [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "Dynamic code check implemented")]
        get
        {
#if NET
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                return AppContext.BaseDirectory;
            }
#endif
            return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                   ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
    
    /// <summary>
    /// Gets or sets the working directory for the test context.
    /// </summary>
    public static string WorkingDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }
    
    /// <summary>
    /// Test name
    /// </summary>
    public string TestName { get; }
    
    /// <summary>
    /// Display name including parameters
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// Test-specific items/data
    /// </summary>
    public Dictionary<string, object?> Items { get; } = new();
    
    /// <summary>
    /// Request information
    /// </summary>
    public TestRequest? Request { get; set; }
    
    /// <summary>
    /// Cancellation token for the test
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
    
    /// <summary>
    /// Test details - simplified for new architecture
    /// </summary>
    public TestDetails? TestDetails { get; set; }
    
    /// <summary>
    /// Test result
    /// </summary>
    public TestResult? Result { get; set; }
    
    /// <summary>
    /// Skip reason if test was skipped
    /// </summary>
    public string? SkipReason { get; set; }
    
    /// <summary>
    /// Class context
    /// </summary>
    public ClassHookContext? ClassContext { get; set; }
    
    /// <summary>
    /// Linked cancellation tokens
    /// </summary>
    public CancellationTokenSource? LinkedCancellationTokens { get; set; }
    
    /// <summary>
    /// Argument display formatters
    /// </summary>
    public List<Func<object?, string?>> ArgumentDisplayFormatters { get; } = new();
    
    /// <summary>
    /// Test context events
    /// </summary>
    public TestContextEvents Events { get; } = new();
    
    /// <summary>
    /// Internal discovered test reference
    /// </summary>
    internal DiscoveredTest? InternalDiscoveredTest { get; set; }
    
    /// <summary>
    /// Service provider (simplified)
    /// </summary>
    private IServiceProvider? _serviceProvider;
    
    public TestContext(string testName, string displayName)
    {
        TestName = testName;
        DisplayName = displayName;
    }
    
    public TestContext(string testName, string displayName, TestRequest request, CancellationToken cancellationToken, IServiceProvider serviceProvider)
    {
        TestName = testName;
        DisplayName = displayName;
        Request = request;
        CancellationToken = cancellationToken;
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Writes to test output
    /// </summary>
    public void WriteLine(string message)
    {
        _outputWriter.WriteLine(message);
    }
    
    /// <summary>
    /// Writes to test error output
    /// </summary>
    public void WriteError(string message)
    {
        _errorWriter.WriteLine(message);
    }
    
    /// <summary>
    /// Gets the captured output
    /// </summary>
    public string GetOutput() => _outputWriter.ToString();
    
    /// <summary>
    /// Gets the captured error output
    /// </summary>
    public string GetErrorOutput() => _errorWriter.ToString();
    
    /// <summary>
    /// Gets service from the service provider
    /// </summary>
    public T? GetService<T>() where T : class
    {
        return _serviceProvider?.GetService(typeof(T)) as T;
    }
    
    /// <summary>
    /// Adds async local values (simplified)
    /// </summary>
    internal void AddAsyncLocalValues(object values)
    {
        // Simplified implementation
    }
    
    /// <summary>
    /// Run on test discovery callback
    /// </summary>
    internal bool RunOnTestDiscovery { get; set; }
    
    /// <summary>
    /// Lock object for thread safety
    /// </summary>
    public object Lock { get; } = new object();
    
    /// <summary>
    /// Test timings
    /// </summary>
    public List<Timing> Timings { get; } = new();
    
    /// <summary>
    /// Test artifacts
    /// </summary>
    public Dictionary<string, object?> Artifacts { get; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Gets the test display name
    /// </summary>
    public string GetTestDisplayName()
    {
        return DisplayName;
    }
    
    /// <summary>
    /// Object bag for storing arbitrary data
    /// </summary>
    public Dictionary<string, object?> ObjectBag { get; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Whether to report this test result
    /// </summary>
    public bool ReportResult { get; set; } = true;
    
    /// <summary>
    /// Gets the standard output captured during test execution
    /// </summary>
    public string? GetStandardOutput()
    {
        return GetOutput();
    }
    
    /// <summary>
    /// Adds a category to the test
    /// </summary>
    public void AddCategory(string category)
    {
        if (TestDetails != null && !TestDetails.Categories.Contains(category))
        {
            TestDetails.Categories.Add(category);
        }
    }
    
    /// <summary>
    /// Adds a custom property to the test
    /// </summary>
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
    
    /// <summary>
    /// Sets the test to not run in parallel
    /// </summary>
    public void SetNotInParallel()
    {
        Items["NotInParallel"] = true;
    }
    
    /// <summary>
    /// Sets the parallel group for the test
    /// </summary>
    public void SetParallelGroup(string groupName, int limit)
    {
        Items["ParallelGroup"] = groupName;
        Items["ParallelGroupLimit"] = limit;
    }
    
    /// <summary>
    /// Sets the retry limit for the test
    /// </summary>
    public void SetRetryLimit(int retryLimit)
    {
        if (TestDetails != null)
        {
            TestDetails.RetryLimit = retryLimit;
        }
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
    /// Sets the argument display formatter type
    /// </summary>
    public void SetArgumentDisplayFormatter(Type formatterType)
    {
        Items["ArgumentDisplayFormatter"] = formatterType;
    }
    
    /// <summary>
    /// Adds an argument display formatter
    /// </summary>
    public void AddArgumentDisplayFormatter(ArgumentDisplayFormatter formatter)
    {
        ArgumentDisplayFormatters.Add(obj => formatter.CanHandle(obj) ? formatter.FormatValue(obj) : null);
    }
    
    /// <summary>
    /// Sets whether to run on test discovery
    /// </summary>
    public void SetRunOnDiscovery(bool runOnDiscovery)
    {
        RunOnTestDiscovery = runOnDiscovery;
    }
    
    /// <summary>
    /// Sets the display name for the test
    /// </summary>
    public void SetDisplayName(string displayName)
    {
        if (TestDetails != null)
        {
            TestDetails.DisplayName = displayName;
        }
    }
    
    /// <summary>
    /// Sets a parallel constraint by key
    /// </summary>
    public void SetParallelConstraint(string constraintKey)
    {
        Items["ParallelConstraint"] = constraintKey;
    }
    
    /// <summary>
    /// Sets a parallel constraint object
    /// </summary>
    public void SetParallelConstraint(object constraint)
    {
        Items["ParallelConstraint"] = constraint;
    }
    
    /// <summary>
    /// Sets the parallel limiter for the test
    /// </summary>
    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        Items["ParallelLimiter"] = parallelLimit;
    }
}

/// <summary>
/// Test request information
/// </summary>
public class TestRequest
{
    public required TestSession Session { get; init; }
}

/// <summary>
/// Test session information
/// </summary>
public class TestSession
{
    public required Guid SessionUid { get; init; }
}