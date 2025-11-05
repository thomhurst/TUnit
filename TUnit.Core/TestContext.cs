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
    ITestExecution, ITestParallelization, ITestOutput, ITestMetadata, ITestDependencies, ITestStateBag, ITestEvents
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

    internal IServiceProvider Services => ServiceProvider;

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

    public static TestContext? GetById(string id) => _testContextsById.GetValueOrDefault(id);

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

    internal string? CustomDisplayName { get; set; }


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

    public object Lock { get; } = new();


    internal IClassConstructor? ClassConstructor => _testBuilderContext.ClassConstructor;

    internal object[]? CachedEligibleEventObjects { get; set; }


    internal ConcurrentDictionary<string, object?> ObjectBag => _testBuilderContext.ObjectBag;


    internal AbstractExecutableTest InternalExecutableTest { get; set; } = null!;

    private ConcurrentDictionary<int, HashSet<object>>? _trackedObjects;
    internal ConcurrentDictionary<int, HashSet<object>> TrackedObjects =>
        _trackedObjects ??= new();


}
