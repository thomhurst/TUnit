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
public class TestContext : Context
{
    private static readonly AsyncLocal<TestContext?> TestContexts = new();
    internal static readonly Dictionary<string, string> InternalParametersDictionary = new();
    
    private readonly StringWriter _outputWriter = new();
    private readonly StringWriter _errorWriter = new();
    
    /// <summary>
    /// Gets or sets the current test context.
    /// </summary>
    public static new TestContext? Current
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
    public TestDetails TestDetails { get; set; } = null!;
    
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
    
    public TestContext(string testName, string displayName) : base(null)
    {
        TestName = testName;
        DisplayName = displayName;
    }
    
    public TestContext(string testName, string displayName, TestRequest request, CancellationToken cancellationToken, IServiceProvider serviceProvider) : base(null)
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
    public new string GetErrorOutput() => _errorWriter.ToString();
    
    /// <summary>
    /// Gets service from the service provider
    /// </summary>
    public T? GetService<T>() where T : class
    {
        return _serviceProvider?.GetService(typeof(T)) as T;
    }
    
    /// <summary>
    /// Adds async local values (delegates to base class)
    /// </summary>
    public new void AddAsyncLocalValues()
    {
        base.AddAsyncLocalValues();
    }
    
    /// <summary>
    /// Restores the async local context for TestContext
    /// </summary>
    internal override void RestoreContextAsyncLocal()
    {
        TestContexts.Value = this;
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