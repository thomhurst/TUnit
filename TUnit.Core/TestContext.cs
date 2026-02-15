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
    ITestExecution, ITestParallelization, ITestOutput, ITestMetadata, ITestDependencies, ITestStateBag, ITestEvents, ITestIsolation
{
    private static readonly ConcurrentDictionary<string, TestContext> _testContextsById = new();
    private readonly TestBuilderContext _testBuilderContext;
    private string? _cachedDisplayName;

    public TestContext(string testName, IServiceProvider serviceProvider, ClassHookContext classContext, TestBuilderContext testBuilderContext, CancellationToken cancellationToken) : base(classContext)
    {
        _testBuilderContext = testBuilderContext;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        ClassContext = classContext;

        // Generate unique ID for this test instance
        Id = Guid.NewGuid().ToString();
        IsolationUniqueId = Interlocked.Increment(ref _isolationIdCounter);

        _testContextsById[Id] = this;
    }

    public string Id { get; }

    // Zero-allocation interface properties for organized API access
    public ITestExecution Execution => this;
    public ITestParallelization Parallelism => this;
    public ITestOutput Output => this;
    public ITestMetadata Metadata => this;
    public ITestDependencies Dependencies => this;
    public ITestStateBag StateBag => this;
    public ITestEvents Events => this;
    public ITestIsolation Isolation => this;

    internal IServiceProvider Services => ServiceProvider;

    private static readonly AsyncLocal<TestContext?> TestContexts = new();

    // Use ConcurrentDictionary for thread-safe access during parallel test discovery
    internal static readonly ConcurrentDictionary<string, List<string>> InternalParametersDictionary = new();

    private StringWriter? _outputWriter;

    private StringWriter? _errorWriter;

    private string? _buildTimeOutput;
    private string? _buildTimeErrorOutput;

    public static new TestContext? Current
    {
        get => TestContexts.Value;
        internal set
        {
            TestContexts.Value = value;
            ClassHookContext.Current = value?.ClassContext;
        }
    }

    public static TestContext? GetById(string id) => _testContextsById.GetValueOrDefault(id);

    public static IReadOnlyDictionary<string, List<string>> Parameters => InternalParametersDictionary;

    private static IConfiguration? _configuration;

    /// <summary>
    /// Gets the test configuration. Throws a descriptive exception if accessed before initialization.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if Configuration is accessed before the test engine initializes it.</exception>
    public static IConfiguration Configuration
    {
        get => _configuration ?? throw new InvalidOperationException(
            "TestContext.Configuration has not been initialized. " +
            "This property is only available after the TUnit test engine has started. " +
            "If you are accessing this from a static constructor or field initializer, " +
            "consider moving the code to a test setup method or test body instead.");
        internal set => _configuration = value;
    }

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

    internal string? CustomDisplayName { get; set; }

    /// <summary>
    /// Display name provided by the data source (from TestDataRow or ArgumentsAttribute.DisplayName).
    /// This takes precedence over the default generated display name but can be overridden by CustomDisplayName.
    /// Supports $paramName substitution.
    /// </summary>
    internal string? DataSourceDisplayName { get; private set; }

    /// <summary>
    /// Sets the display name from the data source (TestDataRow or ArgumentsAttribute.DisplayName).
    /// </summary>
    internal void SetDataSourceDisplayName(string displayName)
    {
        DataSourceDisplayName = displayName;
    }


    internal TestDetails TestDetails { get; set; } = null!;

    internal IParallelLimit? ParallelLimiter { get; set; }

    internal Type? DisplayNameFormatter { get; set; }

    // New: Support multiple parallel constraints
    private List<IParallelConstraint>? _parallelConstraints;


    /// <summary>
    /// Will be null until initialized by TestOrchestrator
    /// </summary>
    public ClassHookContext ClassContext { get; }

    private List<Func<object?, string?>>? _argumentDisplayFormatters;
    internal List<Func<object?, string?>> ArgumentDisplayFormatters =>
        _argumentDisplayFormatters ??= [];


    internal DiscoveredTest? InternalDiscoveredTest { get; set; }

    internal IServiceProvider ServiceProvider
    {
        get;
    }

    internal override void SetAsyncLocalContext()
    {
        TestContexts.Value = this;
    }

    internal bool RunOnTestDiscovery { get; set; }

    /// <summary>
    /// Indicates whether this test is reusing the discovery-time instance instead of creating a new instance.
    /// When true, property resolution and initialization should be skipped since the instance is already prepared.
    /// </summary>
    internal bool IsDiscoveryInstanceReused { get; set; }

    public object Lock { get; } = new();


    internal IClassConstructor? ClassConstructor => _testBuilderContext.ClassConstructor;

    internal object[]? CachedEligibleEventObjects { get; set; }

    // Pre-computed typed event receivers (filtered, sorted, scoped-attribute filtered)
    // These are computed lazily on first access and cached
#if NET
    // Stage-specific caches for .NET 8+ (avoids runtime filtering by stage)
    internal ITestStartEventReceiver[]? CachedTestStartReceiversEarly { get; set; }
    internal ITestStartEventReceiver[]? CachedTestStartReceiversLate { get; set; }
    internal ITestEndEventReceiver[]? CachedTestEndReceiversEarly { get; set; }
    internal ITestEndEventReceiver[]? CachedTestEndReceiversLate { get; set; }
#else
    // Single cache for older frameworks (no stage concept)
    internal ITestStartEventReceiver[]? CachedTestStartReceivers { get; set; }
    internal ITestEndEventReceiver[]? CachedTestEndReceivers { get; set; }
#endif
    internal ITestSkippedEventReceiver[]? CachedTestSkippedReceivers { get; set; }
    internal ITestDiscoveryEventReceiver[]? CachedTestDiscoveryReceivers { get; set; }
    internal ITestRegisteredEventReceiver[]? CachedTestRegisteredReceivers { get; set; }

    // Track the class instance used when building caches for invalidation on retry
    internal object? CachedClassInstance { get; set; }

    /// <summary>
    /// Invalidates all cached event receiver data. Called when class instance changes (e.g., on retry).
    /// </summary>
    internal void InvalidateEventReceiverCaches()
    {
        CachedEligibleEventObjects = null;
#if NET
        CachedTestStartReceiversEarly = null;
        CachedTestStartReceiversLate = null;
        CachedTestEndReceiversEarly = null;
        CachedTestEndReceiversLate = null;
#else
        CachedTestStartReceivers = null;
        CachedTestEndReceivers = null;
#endif
        CachedTestSkippedReceivers = null;
        CachedTestDiscoveryReceivers = null;
        CachedTestRegisteredReceivers = null;
        CachedClassInstance = null;
    }

    internal ConcurrentDictionary<string, object?> ObjectBag => _testBuilderContext.StateBag;

    internal AbstractExecutableTest InternalExecutableTest { get; set; } = null!;

    internal Dictionary<int, HashSet<object>> TrackedObjects { get; } = new();

    /// <summary>
    /// Sets the output captured during test building phase.
    /// This output is prepended to the test's execution output.
    /// </summary>
    internal void SetBuildTimeOutput(string? output, string? errorOutput)
    {
        _buildTimeOutput = output;
        _buildTimeErrorOutput = errorOutput;
    }
}
