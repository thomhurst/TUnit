using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;

namespace TUnit.Core;

/// <summary>
/// Simplified test context for the new architecture
/// </summary>
public class TestContext : Context
{
    public TestContext(string testName, string displayName, CancellationToken cancellationToken, IServiceProvider serviceProvider) : base(null)
    {
        TestName = testName;
        DisplayName = displayName;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
    }

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
    /// Cancellation token for the test
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Test details - simplified for new architecture
    /// </summary>
    public TestDetails TestDetails { get; set; } = null!;

    /// <summary>
    /// Current phase of the test
    /// </summary>
    public TestPhase Phase { get; set; } = TestPhase.Execution;

    /// <summary>
    /// Test result
    /// </summary>
    public TestResult? Result { get; set; }

    /// <summary>
    /// Skip reason if test was skipped
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Parallel limiter for the test
    /// </summary>
    public IParallelLimit? ParallelLimiter { get; private set; }

    /// <summary>
    /// Display name formatter type
    /// </summary>
    public Type? DisplayNameFormatter { get; set; }

    /// <summary>
    /// Retry decision function
    /// </summary>
    public Func<TestContext, Exception, int, Task<bool>>? ShouldRetryFunc { get; set; }

    /// <summary>
    /// Parallel execution constraint
    /// </summary>
    public IParallelConstraint? ParallelConstraint { get; set; }

    /// <summary>
    /// Execution priority (higher values execute first)
    /// </summary>
    public Priority ExecutionPriority { get; set; } = Priority.Normal;

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
    public List<Func<object?, string?>> ArgumentDisplayFormatters { get; } =
    [
    ];

    /// <summary>
    /// Test context events
    /// </summary>
    public TestContextEvents Events { get; } = new();

    /// <summary>
    /// Internal discovered test reference
    /// </summary>
    internal DiscoveredTest? InternalDiscoveredTest { get; set; }

    /// <summary>
    /// Gets the service provider for dependency injection
    /// </summary>
    internal IServiceProvider ServiceProvider
    {
        get;
    }

    public TestContext(string testName, string displayName) : this(testName, displayName, CancellationToken.None, new TestServiceProvider())
    {
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
        return ServiceProvider.GetService(typeof(T)) as T;
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
    public object Lock { get; } = new();

    /// <summary>
    /// Test timings
    /// </summary>
    public List<Timing> Timings { get; } =
    [
    ];

    /// <summary>
    /// Test artifacts
    /// </summary>
    public Dictionary<string, object?> Artifacts { get; } = new();

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
    public Dictionary<string, object?> ObjectBag { get; } = new();

    /// <summary>
    /// Whether to report this test result
    /// </summary>
    public bool ReportResult { get; set; } = true;


    /// <summary>
    /// Sets the parallel limiter for the test
    /// </summary>
    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        ParallelLimiter = parallelLimit;
    }

    /// <summary>
    /// Adds a cancellation token to be linked with the test's cancellation token
    /// </summary>
    public void AddLinkedCancellationToken(CancellationToken cancellationToken)
    {
        if (LinkedCancellationTokens == null)
        {
            LinkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        }
        else
        {
            var existingToken = LinkedCancellationTokens.Token;
            LinkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(existingToken, cancellationToken);
        }

        CancellationToken = LinkedCancellationTokens.Token;
    }

    /// <summary>
    /// Test start time - for compatibility
    /// </summary>
    public DateTimeOffset TestStart { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Adds an artifact to the test
    /// </summary>
    public void AddArtifact(string name, object? value)
    {
        Artifacts[name] = value;
    }

    /// <summary>
    /// Adds an artifact to the test
    /// </summary>
    public void AddArtifact(Artifact artifact)
    {
        Artifacts[artifact.DisplayName ?? artifact.File?.Name ?? "artifact"] = artifact;
    }

    /// <summary>
    /// Overrides the test result
    /// </summary>
    public void OverrideResult(string reason)
    {
        OverrideResult(Status.Passed, reason);
    }

    /// <summary>
    /// Overrides the test result with specified status
    /// </summary>
    public void OverrideResult(Status status, string reason)
    {
        Result = new TestResult
        {
            Status = status,
            OverrideReason = reason,
            IsOverridden = true,
            Start = TestStart,
            End = DateTimeOffset.UtcNow,
            Duration = DateTimeOffset.UtcNow - TestStart,
            Exception = null,
            ComputerName = Environment.MachineName,
            TestContext = this
        };
    }

    /// <summary>
    /// Reregisters a test with new arguments
    /// </summary>
    public async Task ReregisterTestWithArguments(object?[]? methodArguments = null, Dictionary<string, object?>? objectBag = null)
    {
        if (methodArguments != null)
        {
            TestDetails.TestMethodArguments = methodArguments;
        }

        if (objectBag != null)
        {
            foreach (var kvp in objectBag)
            {
                ObjectBag[kvp.Key] = kvp.Value;
            }
        }

        // TODO: This functionality needs to be handled differently now that ITestFinder doesn't support it
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the dependencies for this test
    /// </summary>
    public List<TestDetails> Dependencies { get; } =
    [
    ];

    /// <summary>
    /// Gets tests matching the criteria
    /// </summary>
    public IEnumerable<TestContext> GetTests(Func<TestContext, bool> predicate)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        // Get all tests from the current class and filter using the predicate
        var classType = TestDetails?.ClassType;
        if (classType == null)
        {
            return Enumerable.Empty<TestContext>();
        }

        return testFinder.GetTests(classType).Where(predicate);
    }

    /// <summary>
    /// Gets tests by name from the same test class
    /// </summary>
    public List<TestContext> GetTests(string testName)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        // Use the current test's class type by default
        var classType = TestDetails?.ClassType;
        if (classType == null)
        {
            return new List<TestContext>();
        }

        // Call GetTestsByNameAndParameters with empty parameter lists to get all tests with this name
        return testFinder.GetTestsByNameAndParameters(
            testName,
            Enumerable.Empty<Type>(),
            classType,
            Enumerable.Empty<Type>(),
            Enumerable.Empty<object?>()
        ).ToList();
    }

    /// <summary>
    /// Gets tests by name from a specific test class
    /// </summary>
    public List<TestContext> GetTests(string testName, Type classType)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        // Call GetTestsByNameAndParameters with empty parameter lists to get all tests with this name
        return testFinder.GetTestsByNameAndParameters(
            testName,
            Enumerable.Empty<Type>(),
            classType,
            Enumerable.Empty<Type>(),
            Enumerable.Empty<object?>()
        ).ToList();
    }
}
