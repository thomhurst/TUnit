using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestDiscoveryHookOrchestrator(HooksCollector hooksCollector, string? stringFilter)
{
    private BeforeTestDiscoveryContext? _beforeContext;
    private TestDiscoveryContext? _afterContext;

    public IEnumerable<StaticHookMethod<BeforeTestDiscoveryContext>> CollectBeforeHooks()
    {
        return hooksCollector.BeforeTestDiscoveryHooks
            .OrderBy(x => x.Order);
    }

    public IEnumerable<StaticHookMethod<TestDiscoveryContext>> CollectAfterHooks()
    {
        return hooksCollector.AfterTestDiscoveryHooks
            .OrderBy(x => x.Order);
    }
    
    public BeforeTestDiscoveryContext GetBeforeContext()
    {
        return _beforeContext ??= new BeforeTestDiscoveryContext
        {
            TestFilter = stringFilter
        };
    }

    public TestDiscoveryContext GetAfterContext(IEnumerable<DiscoveredTest> discoveredTests)
    {
        return _afterContext ??= new TestDiscoveryContext(discoveredTests)
        {
            TestFilter = stringFilter
        };
    }
}