using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;

namespace TUnit.Core;

/// <summary>
/// Simplified test context for the new architecture
/// </summary>
[DebuggerDisplay("{TestDetails.ClassType.Name}.{GetDisplayName(),nq}")]
public partial class TestContext : Context,
    ITestExecution, ITestParallelization, ITestOutput, ITestMetadata, ITestDependencies
{
    private static readonly ConcurrentDictionary<Guid, TestContext> _testContextsById = new();
    private readonly TestBuilderContext _testBuilderContext;
    private string? _cachedDisplayName;

    public TestContext(string testName, IServiceProvider serviceProvider, ClassHookContext classContext, TestBuilderContext testBuilderContext, CancellationToken cancellationToken) : base(classContext)
    {
        _testBuilderContext = testBuilderContext;
        TestName = testName;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        ClassContext = classContext;

        _testContextsById[_testBuilderContext.Id] = this;
    }

    public Guid Id => _testBuilderContext.Id;

    // Zero-allocation interface properties for organized API access
    public ITestExecution Execution => this;
    public ITestParallelization Parallelism => this;
    public ITestOutput Output => this;
    public ITestMetadata Metadata => this;
    public ITestDependencies Dependencies => this;
    public IServiceProvider Services => ServiceProvider;

    private static readonly AsyncLocal<TestContext?> TestContexts = new();

    internal static readonly Dictionary<string, List<string>> InternalParametersDictionary = new();

    private StringWriter? _outputWriter;

    private StringWriter? _errorWriter;

    public static new TestContext? Current
    {
        get => TestContexts.Value;
        internal set
        {
            TestContexts.Value = value;
            ClassHookContext.Current = value?.ClassContext;
        }
    }

    public static TestContext? GetById(Guid id) => _testContextsById.GetValueOrDefault(id);

    public static IReadOnlyDictionary<string, List<string>> Parameters => InternalParametersDictionary;

    public static IConfiguration Configuration { get; internal set; } = null!;

    public static string? OutputDirectory
    {
        get
        {
#if NET
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                return AppContext.BaseDirectory;
            }
#endif
            [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "Dynamic code check implemented")]
            string GetOutputDirectory()
            {
                return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                       ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            }

            return GetOutputDirectory();
        }
    }

    public static string WorkingDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }

    public string TestName { get; }

    internal string? CustomDisplayName { get; set; }


    public CancellationToken CancellationToken { get; set; }

    public TestDetails TestDetails { get; set; } = null!;

    public TestPhase Phase { get; set; } = TestPhase.Execution;

    public TestResult? Result { get; set; }

    public string? SkipReason { get; set; }

    public IParallelLimit? ParallelLimiter { get; private set; }

    public Type? DisplayNameFormatter { get; set; }

    /// <summary>
    /// Custom hook executor that overrides the default hook executor for all test-level hooks.
    /// Set via TestRegisteredContext.SetHookExecutor() during test registration.
    /// </summary>
    public IHookExecutor? CustomHookExecutor { get; set; }

    public Func<TestContext, Exception, int, Task<bool>>? RetryFunc { get; set; }

    // New: Support multiple parallel constraints
    private readonly List<IParallelConstraint> _parallelConstraints = [];

    /// <summary>
    /// Gets the collection of parallel constraints applied to this test.
    /// Multiple constraints can be combined (e.g., ParallelGroup + NotInParallel).
    /// </summary>
    public IReadOnlyList<IParallelConstraint> ParallelConstraints => _parallelConstraints;


    public Priority ExecutionPriority { get; set; } = Priority.Normal;

    /// <summary>
    /// The test ID of the parent test, if this test is a variant or child of another test.
    /// Used for tracking test hierarchies in property-based testing shrinking and retry scenarios.
    /// </summary>
    public string? ParentTestId { get; set; }

    /// <summary>
    /// Defines the relationship between this test and its parent test (if ParentTestId is set).
    /// Used by test explorers to display hierarchical relationships.
    /// </summary>
    public TestRelationship Relationship { get; set; } = TestRelationship.None;

    /// <summary>
    /// Will be null until initialized by TestOrchestrator
    /// </summary>
    public ClassHookContext ClassContext { get; }

    public CancellationTokenSource? LinkedCancellationTokens { get; set; }

    public List<Func<object?, string?>> ArgumentDisplayFormatters { get; } =
    [
    ];

    public TestContextEvents Events => _testBuilderContext.Events;

    internal DiscoveredTest? InternalDiscoveredTest { get; set; }

    internal IServiceProvider ServiceProvider
    {
        get;
    }

    public void WriteLine(string message)
    {
        _outputWriter ??= new StringWriter();
        _outputWriter.WriteLine(message);
    }

    public void WriteError(string message)
    {
        _errorWriter ??= new StringWriter();
        _errorWriter.WriteLine(message);
    }

    public string GetOutput() => _outputWriter?.ToString() ?? string.Empty;

    public new string GetErrorOutput() => _errorWriter?.ToString() ?? string.Empty;

    public T? GetService<T>() where T : class
    {
        return ServiceProvider.GetService(typeof(T)) as T;
    }

    internal override void SetAsyncLocalContext()
    {
        TestContexts.Value = this;
    }

    internal bool RunOnTestDiscovery { get; set; }

    public object Lock { get; } = new();

    public ConcurrentBag<Timing> Timings { get; } = [];

    private readonly ConcurrentBag<Artifact> _artifactsBag = new();
    public IReadOnlyList<Artifact> Artifacts => _artifactsBag.ToList();

    internal IClassConstructor? ClassConstructor => _testBuilderContext.ClassConstructor;

    internal object[]? CachedEligibleEventObjects { get; set; }

    public string GetDisplayName()
    {
        if(!string.IsNullOrEmpty(CustomDisplayName))
        {
            return CustomDisplayName!;
        }

        if (_cachedDisplayName != null)
        {
            return _cachedDisplayName;
        }

        if (TestDetails.TestMethodArguments.Length == 0)
        {
            _cachedDisplayName = TestName;
            return TestName;
        }

        var argsLength = TestDetails.TestMethodArguments.Length;
        var sb = StringBuilderPool.Get();
        try
        {
            sb.Append(TestName);
            sb.Append('(');

            for (var i = 0; i < argsLength; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(ArgumentFormatter.Format(TestDetails.TestMethodArguments[i], ArgumentDisplayFormatters));
            }

            sb.Append(')');
            _cachedDisplayName = sb.ToString();
            return _cachedDisplayName;
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Clears the cached display name, forcing it to be recomputed on next access.
    /// This is called after discovery event receivers run to ensure custom argument formatters are applied.
    /// </summary>
    internal void InvalidateDisplayNameCache()
    {
        _cachedDisplayName = null;
    }

    public Dictionary<string, object?> ObjectBag => _testBuilderContext.ObjectBag;

    public bool ReportResult { get; set; } = true;

    public void AddLinkedCancellationToken(CancellationToken cancellationToken)
    {
        if (LinkedCancellationTokens == null)
        {
            LinkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        }
        else
        {
            var existingToken = LinkedCancellationTokens.Token;
            var oldCts = LinkedCancellationTokens;
            LinkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(existingToken, cancellationToken);
            oldCts.Dispose();
        }

        CancellationToken = LinkedCancellationTokens.Token;
    }

    public DateTimeOffset? TestStart { get; set; }

    public void OverrideResult(string reason)
    {
        OverrideResult(TestState.Passed, reason);
    }

    public void OverrideResult(TestState state, string reason)
    {
        Result = new TestResult
        {
            State = state,
            OverrideReason = reason,
            IsOverridden = true,
            Start = TestStart ?? DateTimeOffset.UtcNow,
            End = DateTimeOffset.UtcNow,
            Duration = DateTimeOffset.UtcNow - (TestStart ?? DateTimeOffset.UtcNow),
            Exception = null,
            ComputerName = Environment.MachineName,
            TestContext = this
        };

        InternalExecutableTest.State = state;
    }


    internal AbstractExecutableTest InternalExecutableTest { get; set; } = null!;

    internal ConcurrentDictionary<int, HashSet<object>> TrackedObjects { get; } = [];

    public DateTimeOffset? TestEnd { get; set; }

    public int CurrentRetryAttempt { get; internal set; }


    public IEnumerable<TestContext> GetTests(Func<TestContext, bool> predicate)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        // Get all tests from the current class and filter using the predicate
        var classType = TestDetails?.ClassType;
        if (classType == null)
        {
            return [
            ];
        }

        return testFinder.GetTests(classType).Where(predicate);
    }

    public List<TestContext> GetTests(string testName)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        // Use the current test's class type by default
        var classType = TestDetails.ClassType;

        var tests = testFinder.GetTestsByNameAndParameters(
            testName,
            [
            ],
            classType,
            [
            ],
            [
            ]
        ).ToList();

        if (tests.Any(x => x.Result == null))
        {
            throw new InvalidOperationException(
                "Cannot get unfinished tests - Did you mean to add a [DependsOn] attribute?"
            );
        }

        return tests;
    }

    public List<TestContext> GetTests(string testName, Type classType)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        return testFinder.GetTestsByNameAndParameters(
            testName,
            [
            ],
            classType,
            [
            ],
            [
            ]
        ).ToList();
    }
}
