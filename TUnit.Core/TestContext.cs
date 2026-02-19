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
/// Provides access to the current test's metadata, execution state, output, and configuration.
/// Use <see cref="Current"/> to access the context of the currently executing test.
/// </summary>
/// <remarks>
/// <para>
/// TestContext exposes its functionality through organized interface properties:
/// <see cref="Execution"/> for test lifecycle and result management,
/// <see cref="Output"/> for capturing output and attaching artifacts,
/// <see cref="Metadata"/> for test identity and details,
/// <see cref="Parallelism"/> for parallel execution control,
/// <see cref="Dependencies"/> for test dependency information,
/// <see cref="StateBag"/> for storing custom state during test execution,
/// <see cref="Events"/> for subscribing to test lifecycle events,
/// and <see cref="Isolation"/> for creating unique resource names.
/// </para>
/// </remarks>
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

    /// <summary>
    /// Gets the unique identifier for this test instance.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets access to test execution state, result management, cancellation, and retry information.
    /// </summary>
    public ITestExecution Execution => this;

    /// <summary>
    /// Gets access to parallel execution control and priority configuration.
    /// </summary>
    public ITestParallelization Parallelism => this;

    /// <summary>
    /// Gets access to test output capture, timing, and artifact management.
    /// </summary>
    public ITestOutput Output => this;

    /// <summary>
    /// Gets access to test identity, details, and display name configuration.
    /// </summary>
    public ITestMetadata Metadata => this;

    /// <summary>
    /// Gets access to test dependency information and relationship queries.
    /// </summary>
    public ITestDependencies Dependencies => this;

    /// <summary>
    /// Gets access to a thread-safe bag for storing custom state during test execution.
    /// </summary>
    public ITestStateBag StateBag => this;

    /// <summary>
    /// Gets access to test lifecycle events (registered, start, end, skip, retry, dispose).
    /// </summary>
    public ITestEvents Events => this;

    /// <summary>
    /// Gets access to helpers for creating isolated resource names unique to this test instance.
    /// </summary>
    public ITestIsolation Isolation => this;

    internal IServiceProvider Services => ServiceProvider;

    private static readonly AsyncLocal<TestContext?> TestContexts = new();

    // Use ConcurrentDictionary for thread-safe access during parallel test discovery
    internal static readonly ConcurrentDictionary<string, List<string>> InternalParametersDictionary = new();

    private StringWriter? _outputWriter;

    private StringWriter? _errorWriter;

    private string? _buildTimeOutput;
    private string? _buildTimeErrorOutput;

    /// <summary>
    /// Gets the <see cref="TestContext"/> for the currently executing test, or <c>null</c> if no test is running.
    /// This is an async-local property that is automatically set by the test engine.
    /// </summary>
    /// <example>
    /// <code>
    /// [Test]
    /// public void MyTest()
    /// {
    ///     var context = TestContext.Current!;
    ///     context.Output.WriteLine("Running test: " + context.Metadata.TestName);
    /// }
    /// </code>
    /// </example>
    public static new TestContext? Current
    {
        get => TestContexts.Value;
        internal set
        {
            TestContexts.Value = value;
            ClassHookContext.Current = value?.ClassContext;
        }
    }

    /// <summary>
    /// Gets a <see cref="TestContext"/> by its unique identifier, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the test context.</param>
    /// <returns>The matching <see cref="TestContext"/>, or <c>null</c>.</returns>
    public static TestContext? GetById(string id) => _testContextsById.GetValueOrDefault(id);

    /// <summary>
    /// Gets the dictionary of test parameters indexed by parameter name.
    /// </summary>
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

    /// <summary>
    /// Gets the output directory of the test assembly, or <c>null</c> if it cannot be determined.
    /// This is typically the directory where the test binaries are located.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the current working directory for the test process.
    /// </summary>
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
    /// Gets the class-level hook context for this test, providing access to class-scoped hooks and state.
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

    /// <summary>
    /// Gets a synchronization object that can be used for thread-safe operations within this test context.
    /// </summary>
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
