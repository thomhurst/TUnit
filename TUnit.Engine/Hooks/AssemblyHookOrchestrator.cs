using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.Data;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class AssemblyHookOrchestrator
{
    private static readonly ConcurrentDictionary<Assembly, AssemblyHookContext> AssemblyHookContexts = new();
    
    private static readonly GetOnlyDictionary<Assembly, List<(string Name, StaticHookMethod HookMethod, Lazy<Task> Action)>> SetUps = new();
    private static readonly GetOnlyDictionary<Assembly, List<(string Name, StaticHookMethod HookMethod, Func<Task> Action)>> CleanUps = new();

    private readonly HookMessagePublisher _hookMessagePublisher;
    private readonly GlobalStaticTestHookOrchestrator _globalStaticTestHookOrchestrator;

    public AssemblyHookOrchestrator(HookMessagePublisher hookMessagePublisher,
        GlobalStaticTestHookOrchestrator globalStaticTestHookOrchestrator)
    {
        _hookMessagePublisher = hookMessagePublisher;
        _globalStaticTestHookOrchestrator = globalStaticTestHookOrchestrator;
    }
    
    public static void RegisterBeforeHook(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        var setups = SetUps.GetOrAdd(assembly, _ => []);
        setups.Add((staticMethod.Name, staticMethod, Convert(assembly, staticMethod)));
    }

    public static void RegisterAfterHook(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        var taskFunctions = CleanUps.GetOrAdd(assembly, _ => []);

        taskFunctions.Add((staticMethod.Name, staticMethod, () =>
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

    internal async Task ExecuteBeforeHooks(ExecuteRequestContext executeRequestContext, Assembly assembly, TestContext testContext)
    {
        var context = GetAssemblyHookContext(assembly);
        
        // Run global ones first
        await _globalStaticTestHookOrchestrator.ExecuteBeforeHooks(executeRequestContext, context, testContext.InternalDiscoveredTest);
            
        foreach (var setUp in SetUps.GetOrAdd(assembly, _ => []).OrderBy(x => x.HookMethod.Order))
        {
            // As these are lazy we should always get the same Task
            // So we await the same Task to ensure it's finished first
            // and also gives the benefit of rethrowing the same exception if it failed
            await _hookMessagePublisher.Push(executeRequestContext, $"Before Assembly: {setUp.Name}", setUp.HookMethod, () => setUp.Action.Value);

            await Timings.Record("Class Hook Set Up: " + setUp.Name, testContext, () => setUp.Action.Value);        }
    }

    internal async Task ExecuteCleanups(ExecuteRequestContext executeRequestContext, Assembly assembly, TestContext testContext, List<Exception> cleanUpExceptions)
    {
        if (!InstanceTracker.IsLastTestForAssembly(assembly))
        {
            return;
        }
        
        foreach (var cleanUp in CleanUps.GetOrAdd(assembly, _ => []).OrderBy(x => x.HookMethod.Order))
        {
            await _hookMessagePublisher.Push(executeRequestContext, $"After Assembly: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(cleanUp.Action, cleanUpExceptions));
        }
        
        var context = GetAssemblyHookContext(assembly);
        
        // Run global ones last
        await _globalStaticTestHookOrchestrator.ExecuteAfterHooks(executeRequestContext, context, testContext.InternalDiscoveredTest, cleanUpExceptions);
    }
}