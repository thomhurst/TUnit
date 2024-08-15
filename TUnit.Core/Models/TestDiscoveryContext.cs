namespace TUnit.Core;

public class TestDiscoveryContext
{
    internal TestDiscoveryContext(IEnumerable<AssemblyHookContext> assemblies)
    {
        Assemblies = assemblies;
    }

    public IEnumerable<AssemblyHookContext> Assemblies { get; }
    public IEnumerable<ClassHookContext> TestClasses => Assemblies.SelectMany(x => x.TestClasses);

    public IEnumerable<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests);
}