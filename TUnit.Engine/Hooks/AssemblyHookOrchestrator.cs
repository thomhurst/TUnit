using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TUnit.Engine.Data;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class AssemblyHookOrchestrator
{
    private static readonly ConcurrentDictionary<Assembly, AssemblyHookContext> AssemblyHookContexts = new();
    
    private static readonly GetOnlyDictionary<Assembly, List<(string Name, int Order, Lazy<Task> Action)>> SetUps = new();
    private static readonly GetOnlyDictionary<Assembly, List<(string Name, int Order, Func<Task> Action)>> CleanUps = new();
    
    public static void RegisterBeforeHook(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        var setups = SetUps.GetOrAdd(assembly, _ => []);
        setups.Add((staticMethod.Name, staticMethod.Order, Convert(assembly, staticMethod)));
    }

    public static void RegisterAfterHook(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        var taskFunctions = CleanUps.GetOrAdd(assembly, _ => []);

        taskFunctions.Add((staticMethod.Name, staticMethod.Order, () =>
        {
            var context = GetAssemblyHookContext(assembly);
            
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterAssemblyHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }
    
    public static IEnumerable<AssemblyHookContext> GetAllAssemblyHookContexts() => AssemblyHookContexts.Values;
    
    public static void RegisterTestContext(Assembly assembly, ClassHookContext classHookContext)
    {
        var assemblyHookContext = AssemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
        {
            Assembly = assembly
        });

        assemblyHookContext.TestClasses.Add(classHookContext);
    }

    private static AssemblyHookContext GetAssemblyHookContext(Assembly assembly)
    {
        lock (assembly)
        {
            return AssemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
            {
                Assembly = assembly
            });
        }
    }

    private static Lazy<Task> Convert(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        return new Lazy<Task>(() =>
        {
            var context = GetAssemblyHookContext(assembly);
            
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeAssemblyHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        });
    }

    internal static async Task ExecuteBeforeHooks(Assembly assembly, TestContext testContext)
    {
        var context = GetAssemblyHookContext(assembly);
        
        // Run global ones first
        await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(context, testContext.InternalDiscoveredTest);
            
        foreach (var setUp in SetUps.GetOrAdd(assembly, _ => []).OrderBy(x => x.Order))
        {
            // As these are lazy we should always get the same Task
            // So we await the same Task to ensure it's finished first
            // and also gives the benefit of rethrowing the same exception if it failed
            await Timings.Record("Class Hook Set Up: " + setUp.Name, testContext, () => setUp.Action.Value);        }
    }

    internal static async Task ExecuteCleanups(Assembly assembly, TestContext testContext, List<Exception> cleanUpExceptions)
    {
        if (!InstanceTracker.IsLastTestForAssembly(assembly))
        {
            return;
        }
        
        foreach (var cleanUp in CleanUps.GetOrAdd(assembly, _ => []).OrderBy(x => x.Order))
        {
            await Timings.Record("Assembly Hook Clean Up: " + cleanUp.Name, testContext, () => RunHelpers.RunSafelyAsync(cleanUp.Action, cleanUpExceptions));
        }
        
        var context = GetAssemblyHookContext(assembly);
        
        // Run global ones last
        await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(context, testContext.InternalDiscoveredTest, cleanUpExceptions);
    }
}