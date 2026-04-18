using System.Collections.Concurrent;

namespace TUnit.Core;

public class TestSessionContext : Context
{
    private static readonly AsyncLocal<TestSessionContext?> Contexts = new();
    public static new TestSessionContext? Current
    {
        get => Contexts.Value;
        internal set
        {
            Contexts.Value = value;
            TestDiscoveryContext.Current = value?.TestDiscoveryContext;
        }
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

    private int _failureCount;

    /// <summary>
    /// True if any test in the session completed with Failed, Timeout, or Cancelled state.
    /// Updated atomically by <see cref="MarkFailure"/>; avoids an O(N) AllTests traversal
    /// in after-session hook paths.
    /// </summary>
    internal bool HasFailures => Volatile.Read(ref _failureCount) > 0;

    internal void MarkFailure() => Interlocked.Increment(ref _failureCount);

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
