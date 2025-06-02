using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestSessionHookOrchestrator(HooksCollectorBase hooksCollector)
{
    public async Task RunBeforeTestSession(ExecuteRequestContext executeRequestContext, TestSessionContext testSessionContext)
    {
        var beforeSessionHooks = CollectBeforeHooks();

        foreach (var beforeSessionHook in beforeSessionHooks)
        {
            try
            {
                await beforeSessionHook.ExecuteAsync(testSessionContext, executeRequestContext.CancellationToken);
            }
            catch (Exception e)
            {
                throw new HookFailedException($"Error executing [Before(TestSession)] hook: {beforeSessionHook.MethodInfo.Type.FullName}.{beforeSessionHook.Name}", e);
            }
            
            testSessionContext.RestoreExecutionContext();
        }
    }
    
    public IEnumerable<StaticHookMethod<TestSessionContext>> CollectBeforeHooks()
    {
        return hooksCollector.BeforeTestSessionHooks
            .OrderBy(x => x.Order);
    }

    public IEnumerable<StaticHookMethod<TestSessionContext>> CollectAfterHooks()
    {
        return hooksCollector.AfterTestSessionHooks
            .OrderBy(x => x.Order);
    }
}