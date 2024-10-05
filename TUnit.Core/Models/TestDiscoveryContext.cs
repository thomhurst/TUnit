namespace TUnit.Core;

public class TestDiscoveryContext : Context
{
    private static readonly AsyncLocal<TestDiscoveryContext?> Contexts = new();
    public new static TestDiscoveryContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
    internal TestDiscoveryContext(IEnumerable<AssemblyHookContext> assemblies)
    {
        Assemblies = assemblies;
    }
    
    public required string? TestFilter { get; init; }

    public IEnumerable<AssemblyHookContext> Assemblies { get; }
    public IEnumerable<ClassHookContext> TestClasses => Assemblies.SelectMany(x => x.TestClasses);

    public IEnumerable<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests);
}