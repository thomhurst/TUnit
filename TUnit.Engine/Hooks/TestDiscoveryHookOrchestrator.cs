using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestDiscoveryHookOrchestrator(HooksCollectorBase hooksCollector)
{
    public async Task RunBeforeTestDiscovery(BeforeTestDiscoveryContext beforeTestDiscoveryContext)
    {
        var beforeDiscoveryHooks = CollectBeforeHooks();
        
        foreach (var beforeDiscoveryHook in beforeDiscoveryHooks)
        {
            beforeTestDiscoveryContext.RestoreExecutionContext();

            try
            {
                await beforeDiscoveryHook.ExecuteAsync(beforeTestDiscoveryContext, CancellationToken.None);
            }
            catch (Exception e)
            {
                throw new HookFailedException($"Error executing [Before(TestDiscovery)] hook: {beforeDiscoveryHook.MethodInfo.Type.FullName}.{beforeDiscoveryHook.Name}", e);
            }
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