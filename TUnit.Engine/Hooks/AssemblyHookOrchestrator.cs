using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.Data;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class AssemblyHookOrchestrator(
    HookMessagePublisher hookMessagePublisher,
    GlobalStaticTestHookOrchestrator globalStaticTestHookOrchestrator)
{
    private static readonly ConcurrentDictionary<Assembly, AssemblyHookContext> AssemblyHookContexts = new();
    
    private static readonly GetOnlyDictionary<Assembly, List<(string Name, StaticHookMethod HookMethod, LazyHook<ExecuteRequestContext, HookMessagePublisher> Action)>> SetUps = new();
    private static readonly GetOnlyDictionary<Assembly, List<(string Name, StaticHookMethod HookMethod, Func<Task> Action)>> CleanUps = new();

    public async Task DiscoverHooks(ExecuteRequestContext context)
    {
        foreach (var list in SetUps.Values)
        {
            foreach (var (name, hookMethod, _) in list)
            {
                await hookMessagePublisher.Discover(context, $"Before Assembly: {name}", hookMethod);
            }
        }

        foreach (var list in CleanUps.Values)
        {
            foreach (var (name, hookMethod, _) in list)
            {
                await hookMessagePublisher.Discover(context, $"After Assembly: {name}", hookMethod);
            }
        }
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

    internal static AssemblyHookContext GetAssemblyHookContext(Assembly assembly)
    {
        lock (assembly)
        {
            return AssemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
            {
                Assembly = assembly
            });
        }
    }

    private static LazyHook<ExecuteRequestContext, HookMessagePublisher> Convert(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        return new LazyHook<ExecuteRequestContext, HookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
        {
            var context = GetAssemblyHookContext(assembly);
            
            var timeout = staticMethod.Timeout;

            await hookPublisher.Push(executeRequestContext, $"Before Class: {staticMethod.Name}", staticMethod, () =>
                RunHelpers.RunWithTimeoutAsync(
                    token => staticMethod.HookExecutor.ExecuteBeforeAssemblyHook(staticMethod.MethodInfo, context,
                        () => staticMethod.Body(context, token)), timeout)
            );
        });
    }

    internal async Task ExecuteBeforeHooks(ExecuteRequestContext executeRequestContext, Assembly assembly, TestContext testContext)
    {
        var context = GetAssemblyHookContext(assembly);
        
        // Run global ones first
        await globalStaticTestHookOrchestrator.ExecuteBeforeHooks(executeRequestContext, context);
            
        foreach (var setUp in SetUps.GetOrAdd(assembly, _ => []).OrderBy(x => x.HookMethod.Order))
        {
            // As these are lazy we should always get the same Task
            // So we await the same Task to ensure it's finished first
            // and also gives the benefit of rethrowing the same exception if it failed
            await setUp.Action.Value(executeRequestContext, hookMessagePublisher); 
        }
    }

    internal async Task ExecuteCleanups(ExecuteRequestContext executeRequestContext, Assembly assembly, TestContext testContext, List<Exception> cleanUpExceptions)
    {
        if (!InstanceTracker.IsLastTestForAssembly(assembly))
        {
            return;
        }
        
        foreach (var cleanUp in CleanUps.GetOrAdd(assembly, _ => []).OrderBy(x => x.HookMethod.Order))
        {
            await hookMessagePublisher.Push(executeRequestContext, $"After Assembly: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(cleanUp.Action, cleanUpExceptions));
        }
        
        var context = GetAssemblyHookContext(assembly);
        
        // Run global ones last
        await globalStaticTestHookOrchestrator.ExecuteAfterHooks(executeRequestContext, context, cleanUpExceptions);
    }
}