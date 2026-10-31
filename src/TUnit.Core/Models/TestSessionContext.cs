using System.Collections.Concurrent;

namespace TUnit.Core;

public class TestSessionContext : Context
{
    public static new TestSessionContext? Current
    {
        get => AmbientContexts.Current?.Session;
        // Cascades to the discovery context in a single AsyncLocal write.
        internal set => AmbientContexts.SetSession(value);
    }

    /// <summary>
    /// Global TestBuilderContext for static property initialization.
    /// This context lives for the entire test session and is used for tracking
    /// disposable objects created during static property initialization.
    /// Initialized immediately as a static field to be available before TestSessionContext creation.
    /// </summary>
    public static TestBuilderContext GlobalStaticPropertyContext { get; } = new TestBuilderContext
    {
        TestMetadata = new MethodMetadata
        {
            Type = typeof(object),
            Name = "StaticPropertyInitialization",
            TypeInfo = new ConcreteType(typeof(object)),
            ReturnTypeInfo = new ConcreteType(typeof(void)),
            Parameters = [],
            GenericTypeCount = 0,
            Class = new ClassMetadata
            {
                Name = "GlobalStaticPropertyInitializer",
                Type = typeof(object),
                Namespace = "TUnit.Core",
                TypeInfo = new ConcreteType(typeof(object)),
                Assembly = AssemblyMetadata.GetOrAdd("TUnit.Core", () => new AssemblyMetadata { Name = "TUnit.Core" }),
                Properties = [],
                Parameters = [],
                Parent = null
            }
        },
        Events = new TestContextEvents(),
        StateBag = new ConcurrentDictionary<string, object?>(),
        DataSourceAttribute = null
    };

    internal TestSessionContext(TestDiscoveryContext beforeTestDiscoveryContext) : base(beforeTestDiscoveryContext)
    {
        Current = this;
    }

    public TestDiscoveryContext TestDiscoveryContext => (TestDiscoveryContext) Parent!;

    public required string Id { get; init; }

    public required string? TestFilter { get; init; }

    /// <summary>
    /// The engine-wide cancellation token for this test session. Signalled when the run is
    /// aborted (e.g. Ctrl+C, IDE stop, or process exit). Session-scoped fixtures
    /// (<see cref="Interfaces.IAsyncInitializer"/> implementations such as long-running
    /// containers or distributed applications) can observe this to abort startup and tear
    /// themselves down promptly on an abort request.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="System.Threading.CancellationToken.None"/> until the engine
    /// populates it at the start of execution. Per-test cancellation flows through
    /// <see cref="TestContext.CancellationToken"/> instead; this token spans the whole session.
    /// </remarks>
    public CancellationToken SessionCancellationToken { get; internal set; }

    private readonly Lock _lock = new();
    private readonly List<AssemblyHookContext> _assemblies = [];
    private ClassHookContext[]? _cachedTestClasses;
    private TestContext[]? _cachedAllTests;

    public void AddAssembly(AssemblyHookContext assemblyHookContext)
    {
        lock (_lock)
        {
            _assemblies.Add(assemblyHookContext);
            InvalidateCaches();
        }
    }

    public IReadOnlyList<AssemblyHookContext> Assemblies { get { lock (_lock) return [.. _assemblies]; } }

    public IReadOnlyList<ClassHookContext> TestClasses => _cachedTestClasses ??= Assemblies.SelectMany(x => x.TestClasses).ToArray();

    public IReadOnlyList<TestContext> AllTests => _cachedAllTests ??= TestClasses.SelectMany(x => x.Tests).ToArray();

    private void InvalidateCaches()
    {
        _cachedTestClasses = null;
        _cachedAllTests = null;
    }

    internal bool FirstTestStarted { get; set; }

    internal void InvalidateTestCaches()
    {
        lock (_lock)
        {
            InvalidateCaches();
        }
    }

    private int _failureCount;

    /// <summary>
    /// True if any test in the session completed with Failed, Timeout, or Cancelled state.
    /// Updated atomically by <see cref="MarkFailure"/>; avoids an O(N) AllTests traversal
    /// in after-session hook paths.
    /// </summary>
    internal bool HasFailures => Volatile.Read(ref _failureCount) > 0;

    internal void MarkFailure() => Interlocked.Increment(ref _failureCount);

    private readonly ConcurrentQueue<(Exception Exception, bool Attributable)> _unobservedTaskExceptions = new();

    /// <summary>
    /// Exceptions from faulted tasks that nothing awaited or observed, raised through
    /// <see cref="TaskScheduler.UnobservedTaskException"/> while this session was running.
    /// </summary>
    /// <remarks>
    /// The runtime raises the event from the finalizer thread after the faulted task is garbage
    /// collected, so an exception appears here some time after the fault, not while the test that
    /// caused it is running. Read this in an <c>[After(TestSession)]</c> hook to see the exceptions
    /// recorded so far. With <c>--fail-on-unobserved-task-exceptions</c>, TUnit also runs a final
    /// garbage collection after the session hooks so late faults are not lost.
    /// </remarks>
    public IReadOnlyCollection<Exception> UnobservedTaskExceptions
    {
        get
        {
            var entries = _unobservedTaskExceptions.ToArray();
            var exceptions = new Exception[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                exceptions[i] = entries[i].Exception;
            }

            return exceptions;
        }
    }

    /// <summary>
    /// Each recorded exception, and whether it can be attributed to this session. It cannot when
    /// other sessions were running in the same process when the runtime raised it.
    /// </summary>
    internal (Exception Exception, bool Attributable)[] GetUnobservedTaskExceptionEntries() => _unobservedTaskExceptions.ToArray();

    internal void AddUnobservedTaskException(Exception exception, bool attributable) =>
        _unobservedTaskExceptions.Enqueue((exception, attributable));

    internal readonly List<Artifact> Artifacts = [];

    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }

    internal void RemoveAssembly(AssemblyHookContext assemblyContext)
    {
        lock (_lock)
        {
            _assemblies.Remove(assemblyContext);
            InvalidateCaches();
        }
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
