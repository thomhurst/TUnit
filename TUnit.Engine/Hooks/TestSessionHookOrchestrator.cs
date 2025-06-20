using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestSessionHookOrchestrator(HooksCollectorBase hooksCollector)
{
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private bool _executed;
    
    public async Task RunBeforeTestSession(TestSessionContext testSessionContext, CancellationToken cancellationToken)
    {
        if (_executed)
        {
            return;
        }

        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            if (_executed)
            {
                return;
            }
        
            var beforeSessionHooks = CollectBeforeHooks();

            foreach (var beforeSessionHook in beforeSessionHooks)
            {
                try
                {
                    testSessionContext.RestoreExecutionContext();

                    await beforeSessionHook.ExecuteAsync(testSessionContext, cancellationToken);
                }
                catch (Exception e)
                {
                    throw new HookFailedException($"Error executing [Before(TestSession)] hook: {beforeSessionHook.MethodInfo.Type.FullName}.{beforeSessionHook.Name}", e);
                }
            }
        }
        finally
        {
            _executed = true;
            _semaphoreSlim.Release();
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