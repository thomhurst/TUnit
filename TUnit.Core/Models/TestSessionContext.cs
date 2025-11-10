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

    private readonly List<AssemblyHookContext> _assemblies = [];
    private ClassHookContext[]? _cachedTestClasses;
    private TestContext[]? _cachedAllTests;

    public void AddAssembly(AssemblyHookContext assemblyHookContext)
    {
        _assemblies.Add(assemblyHookContext);
        InvalidateCaches();
    }

    public IReadOnlyList<AssemblyHookContext> Assemblies => _assemblies;

    public IReadOnlyList<ClassHookContext> TestClasses => _cachedTestClasses ??= Assemblies.SelectMany(x => x.TestClasses).ToArray();

    public IReadOnlyList<TestContext> AllTests => _cachedAllTests ??= TestClasses.SelectMany(x => x.Tests).ToArray();

    private void InvalidateCaches()
    {
        _cachedTestClasses = null;
        _cachedAllTests = null;
    }

    internal bool FirstTestStarted { get; set; }

    private readonly List<Artifact> _artifacts = [];

    public IReadOnlyList<Artifact> Artifacts => _artifacts;

    public void AddArtifact(Artifact artifact)
    {
        _artifacts.Add(artifact);
    }

    internal void RemoveAssembly(AssemblyHookContext assemblyContext)
    {
        _assemblies.Remove(assemblyContext);
        InvalidateCaches();
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
