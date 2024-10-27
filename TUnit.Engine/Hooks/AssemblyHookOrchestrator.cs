using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class AssemblyHookOrchestrator(InstanceTracker instanceTracker, HooksCollector hooksCollector)
{
    private readonly ConcurrentDictionary<Assembly, AssemblyHookContext> _assemblyHookContexts = new();

    private readonly GetOnlyDictionary<Assembly, Task> _before = new();
    private readonly GetOnlyDictionary<Assembly, Task> _after = new();

    public async Task ExecuteBeforeHooks(Assembly assembly)
    {
        await _before.GetOrAdd(assembly, async _ =>
            {
                var context = GetContext(assembly);
                
                AssemblyHookContext.Current = context;

                foreach (var beforeEveryClass in hooksCollector.BeforeEveryAssemblyHooks)
                {
                    await beforeEveryClass.Body(context, default);
                }

                var list = hooksCollector.BeforeAssemblyHooks.GetOrAdd(assembly, _ => []);

                foreach (var staticHookMethod in list)
                {
                    await staticHookMethod.Body(context, default);
                }
                
                AssemblyHookContext.Current = null;
            });
    }

    public async Task ExecuteCleanUpsIfLastInstance(
        TestContext testContext, 
        Assembly assembly,
        List<Exception> cleanUpExceptions
        )
    {
        if (!instanceTracker.IsLastTestForAssembly(assembly))
        {
            // Only run one time clean downs when no instances are left!
           return;
        }
        
        await _after.GetOrAdd(assembly, async _ =>
        {
            var context = GetContext(assembly);
            
            AssemblyHookContext.Current = context;
            
            foreach (var testEndEventsObject in testContext.GetLastTestInAssemblyEventObjects())
            {
                await RunHelpers.RunValueTaskSafelyAsync(
                    () => testEndEventsObject.IfLastTestInAssembly(context, testContext),
                    cleanUpExceptions);
            }
            
            var list = hooksCollector.AfterAssemblyHooks.GetOrAdd(assembly, _ => []);

            foreach (var staticHookMethod in list)
            {
                await RunHelpers.RunSafelyAsync(() => staticHookMethod.Body(context, default), cleanUpExceptions);
            }
                
            foreach (var afterEveryAssembly in hooksCollector.AfterEveryAssemblyHooks)
            {
                await RunHelpers.RunSafelyAsync(() => afterEveryAssembly.Body(context, default), cleanUpExceptions);
            }
            
            AssemblyHookContext.Current = null;
        });
    }

    public AssemblyHookContext GetContext(Assembly assembly)
    {
        return _assemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
        {
            Assembly = assembly
        });
    }

    public IEnumerable<AssemblyHookContext> GetAllAssemblyHookContexts()
    {
        return _assemblyHookContexts.Values;
    }
}