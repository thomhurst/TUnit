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
        DiscoveredTests = discoveredTests;
    }

    internal IEnumerable<DiscoveredTest> DiscoveredTests { get; }

    public IEnumerable<TestContext> AllTests => DiscoveredTests.Select(x => x.TestContext);
}