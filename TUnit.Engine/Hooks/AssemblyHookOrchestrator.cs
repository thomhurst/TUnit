using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Extensions;
using TUnit.Core.Hooks;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class AssemblyHookOrchestrator(InstanceTracker instanceTracker, HooksCollectorBase hooksCollector, ContextManager contextManager, TestSessionHookOrchestrator testSessionHookOrchestrator)
{
    private readonly ConcurrentDictionary<Assembly, bool> _beforeHooksReached = new();
    
    internal GetOnlyDictionary<Assembly, TaskCompletionSource<bool>> PreviouslyRunBeforeHooks { get; } = new();

    public async Task ExecuteBeforeAssemblyHooks(TestContext testContext)
    {
        var assemblyHookContext = testContext.AssemblyContext;

        var assemblyHooksTaskCompletionSource = PreviouslyRunBeforeHooks.GetOrAdd(
            testContext.TestDetails.TestClass.Type.Assembly, _ => new TaskCompletionSource<bool>(),
            out var assemblyHooksTaskPreviouslyExisted);
        
        if (assemblyHooksTaskPreviouslyExisted)
        {
            await assemblyHooksTaskCompletionSource.Task;
            
            assemblyHookContext.RestoreExecutionContext();
            
            return;
        }

        try
        {
            var beforeAssemblyHooks = CollectBeforeHooks(testContext.TestDetails.TestClass.Type.Assembly);

            await testSessionHookOrchestrator.RunBeforeTestSession(testContext.AssemblyContext.TestSessionContext, testContext.CancellationToken);
            
            AssemblyHookContext.Current = assemblyHookContext;

            foreach (var beforeHook in beforeAssemblyHooks)
            {
                assemblyHookContext.RestoreExecutionContext();

                try
                {
                    await beforeHook.ExecuteAsync(assemblyHookContext, CancellationToken.None);
                }
                catch (Exception e)
                {
                    throw new HookFailedException($"Error executing [Before(Assembly)] hook: {beforeHook.MethodInfo.Type.FullName}.{beforeHook.Name}", e);
                }
            }

            AssemblyHookContext.Current = null;
            assemblyHooksTaskCompletionSource.SetResult(false);
        }
        catch (Exception e)
        {
            assemblyHooksTaskCompletionSource.SetException(e);
            throw;
        }
    }
    
    public IEnumerable<StaticHookMethod<AssemblyHookContext>> CollectBeforeHooks(Assembly assembly)
    {
        _beforeHooksReached.GetOrAdd(assembly, true);
        
        var context = contextManager.GetAssemblyHookContext(assembly);
        
        AssemblyHookContext.Current = context;

        foreach (var beforeEveryAssembly in hooksCollector.BeforeEveryAssemblyHooks
                     .OrderBy(x => x.Order))
        {
            yield return beforeEveryAssembly;
        }

        var list = hooksCollector.BeforeAssemblyHooks
            .GetOrAdd(assembly, _ => []).OrderBy(x => x.Order);

        foreach (var staticHookMethod in list)
        {
            yield return staticHookMethod;
        }
    }

    public IEnumerable<IExecutableHook<AssemblyHookContext>> CollectAfterHooks(
        TestContext testContext, 
        Assembly assembly
        )
    {
        if (!instanceTracker.IsLastTestForAssembly(assembly))
        {
            // Only run one time clean downs when no instances are left!
           yield break;
        }
        
        if (!_beforeHooksReached.TryGetValue(assembly, out _))
        {
            // The before hooks were never hit, meaning no tests were executed, so nothing to clean up.
            yield break;
        }
        
        foreach (var testEndEventsObject in testContext.GetLastTestInAssemblyEventObjects())
        {
            yield return new LastTestInAssemblyAdapter(testEndEventsObject, testContext);
        }
            
        var list = hooksCollector.AfterAssemblyHooks.GetOrAdd(assembly, _ => []).OrderBy(x => x.Order);

        foreach (var staticHookMethod in list)
        {
            yield return staticHookMethod;
        }

        foreach (var afterEveryAssembly in hooksCollector.AfterEveryAssemblyHooks.OrderBy(x => x.Order))
        {
            yield return afterEveryAssembly;
        }
    }
}