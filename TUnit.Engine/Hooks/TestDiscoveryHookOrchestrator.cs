using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Logging;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestDiscoveryHookOrchestrator(HooksCollectorBase hooksCollector, string? stringFilter)
{
    private BeforeTestDiscoveryContext? _beforeContext;
    private TestDiscoveryContext? _afterContext;
    
    

    public async Task<ExecutionContext?> RunBeforeTestDiscovery()
    {
        hooksCollector.CollectDiscoveryHooks();
        
        var beforeDiscoveryHooks = CollectBeforeHooks();
        var beforeContext = GetBeforeContext();
        
        foreach (var beforeDiscoveryHook in beforeDiscoveryHooks)
        {
            try
            {
                await beforeDiscoveryHook.ExecuteAsync(beforeContext, CancellationToken.None);
            }
            catch (Exception e)
            {
                throw new HookFailedException($"Error executing [Before(TestDiscovery)] hook: {beforeDiscoveryHook.MethodInfo.Type.FullName}.{beforeDiscoveryHook.Name}", e);
            }
            
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