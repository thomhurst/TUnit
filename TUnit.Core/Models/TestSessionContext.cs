namespace TUnit.Core;

public class TestSessionContext : Context
{
    private static readonly AsyncLocal<TestSessionContext?> Contexts = new();
    public new static TestSessionContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
    internal TestSessionContext(IEnumerable<AssemblyHookContext> assemblies)
    {
        Assemblies = assemblies;
        Current = this;
    }
    
    public required string? TestFilter { get; init; }

    public IEnumerable<AssemblyHookContext> Assemblies { get; }
    public IEnumerable<ClassHookContext> TestClasses => Assemblies.SelectMany(x => x.TestClasses);

    public IEnumerable<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests);

    internal readonly List<Artifact> Artifacts = [];

    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
}