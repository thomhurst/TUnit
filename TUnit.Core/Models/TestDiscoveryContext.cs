namespace TUnit.Core;

public class TestDiscoveryContext : Context
{
    private static readonly AsyncLocal<TestDiscoveryContext?> Contexts = new();
    public new static TestDiscoveryContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
    internal TestDiscoveryContext(IEnumerable<DiscoveredTest> discoveredTests)
    {
        var classContexts = discoveredTests.GroupBy(x => x.TestDetails.ClassType).Select(x => new ClassHookContext()
        {
            ClassType = x.Key,
            Tests = [..x.Select(dt => dt.TestContext)]
        });

        var assemblyContexts = classContexts.GroupBy(x => x.ClassType.Assembly).Select(x => new AssemblyHookContext()
        {
            Assembly = x.Key,
            TestClasses = [..x]
        });

        Assemblies = assemblyContexts;
        Current = this;
    }

    public required string? TestFilter { get; init; }

    public IEnumerable<AssemblyHookContext> Assemblies { get; }
    public IEnumerable<ClassHookContext> TestClasses => Assemblies.SelectMany(x => x.TestClasses);

    public IEnumerable<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests);
}