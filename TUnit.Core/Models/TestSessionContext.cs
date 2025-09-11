namespace TUnit.Core;

public class TestSessionContext : Context
{
    private static readonly AsyncLocal<TestSessionContext?> Contexts = new();
    public static new TestSessionContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
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
            TypeReference = TypeReference.CreateConcrete(typeof(object).AssemblyQualifiedName ?? "System.Object"),
            ReturnTypeReference = TypeReference.CreateConcrete(typeof(void).AssemblyQualifiedName ?? "System.Void"),
            Parameters = Array.Empty<ParameterMetadata>(),
            GenericTypeCount = 0,
            Class = new ClassMetadata
            {
                Name = "GlobalStaticPropertyInitializer",
                Type = typeof(object),
                Namespace = "TUnit.Core",
                TypeReference = TypeReference.CreateConcrete(typeof(object).AssemblyQualifiedName ?? "System.Object"),
                Assembly = AssemblyMetadata.GetOrAdd("TUnit.Core", () => new AssemblyMetadata { Name = "TUnit.Core" }),
                Properties = Array.Empty<PropertyMetadata>(),
                Parameters = Array.Empty<ParameterMetadata>(),
                Parent = null
            }
        },
        Events = new TestContextEvents(),
        ObjectBag = new Dictionary<string, object?>(),
        DataSourceAttribute = null
    };

    internal TestSessionContext(TestDiscoveryContext beforeTestDiscoveryContext) : base(beforeTestDiscoveryContext)
    {
        Current = this;
    }

    public BeforeTestDiscoveryContext TestDiscoveryContext => (BeforeTestDiscoveryContext) Parent!;

    public required string Id { get; init; }

    public required string? TestFilter { get; init; }

    private readonly List<AssemblyHookContext> _assemblies = [];

    public void AddAssembly(AssemblyHookContext assemblyHookContext)
    {
        _assemblies.Add(assemblyHookContext);
    }

    public IReadOnlyList<AssemblyHookContext> Assemblies => _assemblies;

    public IReadOnlyList<ClassHookContext> TestClasses => Assemblies.SelectMany(x => x.TestClasses).ToArray();

    public IReadOnlyList<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests).ToArray();

    internal bool FirstTestStarted { get; set; }

    internal readonly List<Artifact> Artifacts = [];

    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }

    internal void RemoveAssembly(AssemblyHookContext assemblyContext)
    {
        _assemblies.Remove(assemblyContext);
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
