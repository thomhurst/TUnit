using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Logging;
using TUnit.Engine.Helpers;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestDiscoveryHookOrchestrator(HooksCollector hooksCollector, TUnitFrameworkLogger logger, string? stringFilter)
{
    private BeforeTestDiscoveryContext? _beforeContext;
    private TestDiscoveryContext? _afterContext;

    public async Task<ExecutionContext?> RunBeforeTestDiscovery()
    {
        var beforeDiscoveryHooks = CollectBeforeHooks();
        var beforeContext = GetBeforeContext();
        
        foreach (var beforeDiscoveryHook in beforeDiscoveryHooks)
        {
            await logger.LogDebugAsync("Executing [Before(TestDiscovery)] hook");

            await beforeDiscoveryHook.ExecuteAsync(beforeContext, CancellationToken.None);
            
            ExecutionContextHelper.RestoreContext(beforeContext.ExecutionContext);
        }

        return beforeContext.ExecutionContext;
    }
    
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