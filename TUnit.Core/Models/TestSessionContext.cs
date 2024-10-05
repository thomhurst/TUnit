namespace TUnit.Core;

public class TestSessionContext : TestDiscoveryContext
{
    private static readonly AsyncLocal<TestSessionContext?> Contexts = new();
    public new static TestSessionContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
    internal TestSessionContext(IEnumerable<AssemblyHookContext> assemblies) : base(assemblies)
    {
        Current = this;
    }

    internal readonly List<Artifact> Artifacts = [];

    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
}