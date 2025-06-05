using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class TestSessionContext : Context
{
    private static readonly AsyncLocal<TestSessionContext?> Contexts = new();
    public static new TestSessionContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
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
    
    internal override void RestoreContextAsyncLocal()
    {
        Current = this;
    }
}