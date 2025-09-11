using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
public class TestContext : Context
{
    private readonly TestBuilderContext _testBuilderContext;

    public TestContext(string testName, IServiceProvider serviceProvider, ClassHookContext classContext, TestBuilderContext testBuilderContext, CancellationToken cancellationToken) : base(classContext)
    {
        _testBuilderContext = testBuilderContext;
        TestName = testName;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        ClassContext = classContext;
    }

    private static readonly AsyncLocal<TestContext?> TestContexts = new();

    internal static readonly Dictionary<string, List<string>> InternalParametersDictionary = new();

    private readonly StringWriter _outputWriter = new();

    private readonly StringWriter _errorWriter = new();

    public static new TestContext? Current
    {
        get => TestContexts.Value;
        internal set => TestContexts.Value = value;
    }

    public static IReadOnlyDictionary<string, List<string>> Parameters => InternalParametersDictionary;

    public static IConfiguration Configuration { get; internal set; } = null!;

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

    public Func<TestContext, Exception, int, Task<bool>>? RetryFunc { get; set; }

    public IParallelConstraint? ParallelConstraint { get; set; }

    public Priority ExecutionPriority { get; set; } = Priority.Normal;

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
        _outputWriter.WriteLine(message);
    }

    public void WriteError(string message)
    {
        _errorWriter.WriteLine(message);
    }

    public string GetOutput() => _outputWriter.ToString();

    public new string GetErrorOutput() => _errorWriter.ToString();

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

    public ConcurrentBag<Timing> Timings { get; } = new();

    public IReadOnlyList<Artifact> Artifacts { get; } = new List<Artifact>();

    internal IClassConstructor? ClassConstructor => _testBuilderContext.ClassConstructor;

    public string GetDisplayName()
    {
        if(!string.IsNullOrEmpty(CustomDisplayName))
        {
            return CustomDisplayName!;
        }

        var arguments = string.Join(", ", TestDetails.TestMethodArguments
            .Select(arg => ArgumentFormatter.Format(arg, ArgumentDisplayFormatters)));

        if (string.IsNullOrEmpty(arguments))
        {
            return TestName;
        }

        return $"{TestName}({arguments})";
    }

    public Dictionary<string, object?> ObjectBag => _testBuilderContext.ObjectBag;

    public bool ReportResult { get; set; } = true;


    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        ParallelLimiter = parallelLimit;
    }

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

    public DateTimeOffset TestStart { get; set; } = DateTimeOffset.UtcNow;

    public void AddArtifact(Artifact artifact)
    {
        ((List<Artifact>)Artifacts).Add(artifact);
    }

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
            Start = TestStart,
            End = DateTimeOffset.UtcNow,
            Duration = DateTimeOffset.UtcNow - TestStart,
            Exception = null,
            ComputerName = Environment.MachineName,
            TestContext = this
        };

        InternalExecutableTest.State = state;
    }

    /// <summary>
    /// Reregisters a test with new arguments. This method is currently non-functional as the underlying
    /// ITestFinder interface has been removed. This functionality may be reimplemented in a future version.
    /// </summary>
    /// <remarks>
    /// Previously used for dynamically modifying test arguments at runtime. Consider using data source
    /// attributes for parameterized tests instead.
    /// </remarks>
    [Obsolete("This method is non-functional after the removal of ITestFinder. It will be removed in a future version.")]
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

        // This method is currently non-functional - see Obsolete attribute above
        await Task.CompletedTask;
    }

    public List<TestDetails> Dependencies { get; } =
    [
    ];

    internal AbstractExecutableTest InternalExecutableTest { get; set; } = null!;
    public DateTimeOffset? TestEnd { get; set; }


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

        // Call GetTestsByNameAndParameters with empty parameter lists to get all tests with this name
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

        // Call GetTestsByNameAndParameters with empty parameter lists to get all tests with this name
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
