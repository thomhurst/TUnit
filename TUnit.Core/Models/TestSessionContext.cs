namespace TUnit.Core;

public class TestSessionContext : TestDiscoveryContext
{
    internal TestSessionContext(IEnumerable<AssemblyHookContext> assemblies) : base(assemblies)
    {
    }

    internal readonly List<Artifact> Artifacts = [];

    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
}