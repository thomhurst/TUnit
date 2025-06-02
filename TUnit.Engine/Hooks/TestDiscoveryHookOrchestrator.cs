using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestDiscoveryHookOrchestrator(HooksCollectorBase hooksCollector)
{
    public async Task RunBeforeTestDiscovery(BeforeTestDiscoveryContext beforeTestDiscoveryContext)
    {
        hooksCollector.CollectHooks();
        
        var beforeDiscoveryHooks = CollectBeforeHooks();
        
        foreach (var beforeDiscoveryHook in beforeDiscoveryHooks)
        {
            try
            {
                await beforeDiscoveryHook.ExecuteAsync(beforeTestDiscoveryContext, CancellationToken.None);
            }
            catch (Exception e)
            {
                throw new HookFailedException($"Error executing [Before(TestDiscovery)] hook: {beforeDiscoveryHook.MethodInfo.Type.FullName}.{beforeDiscoveryHook.Name}", e);
            }
            
            beforeTestDiscoveryContext.RestoreExecutionContext();
        }
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
}