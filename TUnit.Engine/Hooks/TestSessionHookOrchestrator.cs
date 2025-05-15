using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Logging;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestSessionHookOrchestrator(HooksCollectorBase hooksCollector, AssemblyHookOrchestrator assemblyHookOrchestrator, string? stringFilter)
{
    private TestSessionContext? _context;
    
    public async Task<ExecutionContext?> RunBeforeTestSession(ExecuteRequestContext executeRequestContext)
    {
        hooksCollector.CollectionTestSessionHooks();
        
        var testSessionContext = GetContext(executeRequestContext);
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
            
            ExecutionContextHelper.RestoreContext(testSessionContext.ExecutionContext);
        }
        
        // After Discovery and Before test session hooks are run, more chance of referenced assemblies
        // being loaded into the AppDomain, so now we collect the test hooks which should pick up loaded libraries too
        hooksCollector.CollectHooks();

        return testSessionContext.ExecutionContext;
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
    
    public TestSessionContext GetContext(ExecuteRequestContext executeRequestContext)
    {
        return _context ??= new TestSessionContext(assemblyHookOrchestrator.GetAllAssemblyHookContexts())
        {
            TestFilter = stringFilter,
            Id = executeRequestContext.Request.Session.SessionUid.Value
        };
    }
}