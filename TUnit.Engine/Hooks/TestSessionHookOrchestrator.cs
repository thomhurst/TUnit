﻿using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Logging;
using TUnit.Engine.Helpers;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class TestSessionHookOrchestrator(HooksCollector hooksCollector, AssemblyHookOrchestrator assemblyHookOrchestrator, TUnitFrameworkLogger logger, string? stringFilter)
{
    private TestSessionContext? _context;
    
    public async Task<ExecutionContext?> RunBeforeTestSession(CancellationToken cancellationToken)
    {
        hooksCollector.CollectionTestSessionHooks();
        
        var testSessionContext = GetContext();
        var beforeSessionHooks = CollectBeforeHooks();

        foreach (var beforeSessionHook in beforeSessionHooks)
        {
            await logger.LogDebugAsync("Executing [Before(TestSession)] hook");

            await beforeSessionHook.ExecuteAsync(testSessionContext, cancellationToken);
            
            ExecutionContextHelper.RestoreContext(testSessionContext.ExecutionContext);
        }
        
        // After Discovery and Before test session hooks are run, more chance of references assemblies
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
    
    public TestSessionContext GetContext()
    {
        return _context ??= new TestSessionContext(assemblyHookOrchestrator.GetAllAssemblyHookContexts())
        {
            TestFilter = stringFilter
        };
    }
}