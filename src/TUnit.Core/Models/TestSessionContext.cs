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
    /// Builder context for static property initialization in the current session.
    /// Falls back to a process context before a session exists. Both generated and reflection
    /// data sources use this accessor so overlapping sessions keep separate disposal callbacks.
    /// </summary>
    public static TestBuilderContext GlobalStaticPropertyContext => Current?.StaticPropertyContext ?? s_globalStaticPropertyContext;

    internal TestBuilderContext StaticPropertyContext { get; } = new()
    {
        TestMetadata = s_globalStaticPropertyContext.TestMetadata
    };

    private static readonly TestBuilderContext s_globalStaticPropertyContext = new TestBuilderContext
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

    /// <summary>
    /// OnDispose callbacks that data sources subscribed while a generic class's rows were evaluated to infer
    /// its type arguments (by reflection discovery, or by a source-generated row partition), raised once when
    /// this session ends (#7205). They belong to this session, not the process, so a concurrent session in
    /// server mode never raises them.
    /// </summary>
    internal AsyncEvent<TestContext>? DeferredDiscoveryOnDispose { get; private set; }

    // Guards DeferredDiscoveryOnDispose and the static property context's OnDispose: builds add to them in parallel.
    private static readonly object s_deferredOnDisposeLock = new();

    /// <summary>
    /// Moves the OnDispose callbacks subscribed on <paramref name="context"/> to the current session's
    /// <see cref="DeferredDiscoveryOnDispose"/>, so they are raised once when that session ends. Rows evaluated
    /// to infer a generic class's type arguments are not owned by one test (reflection discovery shares them
    /// across the partition's tests), so a dependency injection scope created while they were read lives for
    /// the session (#7205). Without a current session they go to <see cref="GlobalStaticPropertyContext"/>,
    /// which the session's static property cleanup raises.
    /// </summary>
    internal static void DeferOnDisposeToSessionEnd(TestBuilderContext context)
        => DeferOnDisposeToSessionEnd(context.EventsIfCreated);

    /// <summary>
    /// Moves the OnDispose callbacks subscribed on <paramref name="events"/> to the current session's
    /// <see cref="DeferredDiscoveryOnDispose"/>, as <see cref="DeferOnDisposeToSessionEnd(TestBuilderContext)"/> does.
    /// </summary>
    internal static void DeferOnDisposeToSessionEnd(TestContextEvents? events)
    {
        lock (s_deferredOnDisposeLock)
        {
            if (events is not { OnDispose: { } onDispose })
            {
                return;
            }

            events.OnDispose = null;

            AsyncEvent<TestContext> target;
            if (Current is { } session)
            {
                target = session.DeferredDiscoveryOnDispose ??= new AsyncEvent<TestContext>();
            }
            else
            {
                var globalEvents = GlobalStaticPropertyContext.Events;
                target = globalEvents.OnDispose ??= new AsyncEvent<TestContext>();
            }

            foreach (var invocation in onDispose.InvocationList)
            {
                target.Add(invocation.InvokeAsync, invocation.Order);
            }
        }
    }

    /// <summary>
    /// Takes the callbacks <see cref="DeferOnDisposeToSessionEnd"/> moved to this session, so they are raised once.
    /// </summary>
    internal AsyncEvent<TestContext>? TakeDeferredDiscoveryOnDispose()
    {
        lock (s_deferredOnDisposeLock)
        {
            var onDispose = DeferredDiscoveryOnDispose;
            DeferredDiscoveryOnDispose = null;
            return onDispose;
        }
    }

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
