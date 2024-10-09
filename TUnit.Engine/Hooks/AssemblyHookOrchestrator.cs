using System.Reflection;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class AssemblyHookOrchestrator(
    HookMessagePublisher hookMessagePublisher,
    GlobalStaticTestHookOrchestrator globalStaticTestHookOrchestrator)
{
    public async Task DiscoverHooks(ExecuteRequestContext context)
    {
        foreach (var list in TestDictionary.AssemblySetUps.Values)
        {
            foreach (var (name, hookMethod, _) in list)
            {
                await hookMessagePublisher.Discover(context.Request.Session.SessionUid.Value, $"Before Assembly: {name}", hookMethod);
            }
        }

        foreach (var list in TestDictionary.AssemblyCleanUps.Values)
        {
            foreach (var (name, hookMethod, _) in list)
            {
                await hookMessagePublisher.Discover(context.Request.Session.SessionUid.Value, $"After Assembly: {name}", hookMethod);
            }
        }
    }
    
    public static IEnumerable<AssemblyHookContext> GetAllAssemblyHookContexts() => TestDictionary.AssemblyHookContexts.Values;

    internal static AssemblyHookContext GetAssemblyHookContext(Assembly assembly)
    {
        lock (assembly)
        {
            return TestDictionary.AssemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
            {
                Assembly = assembly
            });
        }
    }

    private static LazyHook<string, IHookMessagePublisher> Convert(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        return new LazyHook<string, IHookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
        {
            var context = GetAssemblyHookContext(assembly);
            
            var timeout = staticMethod.Timeout;

            await hookPublisher.Push(executeRequestContext, $"Before Assembly: {staticMethod.Name}", staticMethod, () =>
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
            
        foreach (var setUp in TestDictionary.AssemblySetUps.GetOrAdd(assembly, _ => []).OrderBy(x => x.HookMethod.Order))
        {
            // As these are lazy we should always get the same Task
            // So we await the same Task to ensure it's finished first
            // and also gives the benefit of rethrowing the same exception if it failed
            await setUp.Action.Value(executeRequestContext.Request.Session.SessionUid.Value, hookMessagePublisher); 
        }
    }

    internal async Task ExecuteCleanupsIfLastInstance(ExecuteRequestContext executeRequestContext, TestContext testContext1,
        Assembly assembly, TestContext testContext, List<Exception> cleanUpExceptions)
    {
        if (!InstanceTracker.IsLastTestForAssembly(assembly))
        {
            return;
        }
        
        foreach (var cleanUp in TestDictionary.AssemblyCleanUps.GetOrAdd(assembly, _ => []).OrderBy(x => x.HookMethod.Order))
        {
            await hookMessagePublisher.Push(executeRequestContext.Request.Session.SessionUid.Value, $"After Assembly: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(cleanUp.Action, cleanUpExceptions));
        }
        
        var context = GetAssemblyHookContext(assembly);
        
        await RunHelpers.RunSafelyAsync(async () =>
        {
            foreach (var testEndEventsObject in testContext.GetTestEndEventsObjects())
            {
                await testEndEventsObject.IfLastTestInAssembly(context, testContext);
            }
        }, cleanUpExceptions);
        
        // Run global ones last
        await globalStaticTestHookOrchestrator.ExecuteAfterHooks(executeRequestContext, context, cleanUpExceptions);
    }
}