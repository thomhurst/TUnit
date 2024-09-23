using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class GlobalStaticTestHookOrchestrator(HookMessagePublisher hookMessagePublisher)
{
    public async Task DiscoverHooks(ExecuteRequestContext context)
    {
        foreach (var (name, hookMethod, _) in TestDictionary.GlobalClassSetUps)
        {
            await hookMessagePublisher.Discover(context, $"Before Class: {name}", hookMethod);
        }
        
        foreach (var (name, hookMethod, _) in TestDictionary.GlobalClassCleanUps)
        {
            await hookMessagePublisher.Discover(context, $"After Class: {name}", hookMethod);
        }
        
        foreach (var (name, hookMethod, _) in TestDictionary.GlobalAssemblySetUps)
        {
            await hookMessagePublisher.Discover(context, $"Before Assembly: {name}", hookMethod);
        }
        
        foreach (var (name, hookMethod, _) in TestDictionary.GlobalAssemblyCleanUps)
        {
            await hookMessagePublisher.Discover(context, $"After Assembly: {name}", hookMethod);
        }
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<TestContext> staticMethod)
    {
        TestDictionary.GlobalTestSetUps.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                await RunHelpers.RunWithTimeoutAsync(token => HookExecutorProvider.GetHookExecutor(staticMethod, context.InternalDiscoveredTest).ExecuteBeforeTestHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new BeforeTestException($"Error executing BeforeEvery(Test) method: {staticMethod.Name}", e);
            }
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<TestContext> staticMethod)
    {
        TestDictionary.GlobalTestCleanUps.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                await RunHelpers.RunWithTimeoutAsync(token => HookExecutorProvider.GetHookExecutor(staticMethod, context.InternalDiscoveredTest).ExecuteAfterTestHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterTestException($"Error executing AfterEvery(Test) method: {staticMethod.Name}", e);
            }
        }));
    }

    internal static async Task ExecuteBeforeHooks(DiscoveredTest discoveredTest)
    {
        foreach (var setUp in TestDictionary.GlobalTestSetUps.OrderBy(x => x.HookMethod.Order))
        {
            await Timings.Record("Global Static Test Hook Set Up: " + setUp.Name, discoveredTest.TestContext, 
                () => setUp.Action(discoveredTest.TestContext));
        }
    }

    internal static async Task ExecuteAfterHooks(DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in TestDictionary.GlobalTestCleanUps.OrderBy(x => x.HookMethod.Order))
        {
            await Timings.Record("Global Static Test Hook Clean Up: " + cleanUp.Name, discoveredTest.TestContext,
                () => RunHelpers.RunSafelyAsync(async () => await cleanUp.Action(discoveredTest.TestContext), cleanUpExceptions));
        }
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<ClassHookContext> staticMethod)
    {
        TestDictionary.GlobalClassSetUps.Add((staticMethod.Name, staticMethod, new LazyHook<ExecuteRequestContext, HookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
        {
            var timeout = staticMethod.Timeout;

            var classHookContext = ClassHookOrchestrator.GetClassHookContext(staticMethod.ClassType);

            try
            {
                ClassHookContext.Current = classHookContext;
                
                await hookPublisher.Push(executeRequestContext, $"Before Class: {staticMethod.Name}",
                    staticMethod, () =>
                        RunHelpers.RunWithTimeoutAsync(
                            token => staticMethod.HookExecutor.ExecuteBeforeClassHook(staticMethod.MethodInfo,
                                classHookContext,
                                () => staticMethod.Body(classHookContext, token)), timeout)
                );
            }
            catch (Exception e)
            {
                throw new BeforeClassException($"Error executing Before(Class) method: {staticMethod.Name}", e);
            }
            finally
            {
                ClassHookContext.Current = null;
            }
        })));
    }

    public static void RegisterAfterHook(StaticHookMethod<ClassHookContext> staticMethod)
    {
        TestDictionary.GlobalClassCleanUps.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                ClassHookContext.Current = context;
                
                await RunHelpers.RunWithTimeoutAsync(
                    token => staticMethod.HookExecutor.ExecuteAfterClassHook(staticMethod.MethodInfo, context,
                        () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterClassException($"Error executing After(Class) method: {staticMethod.Name}", e);
            }
            finally
            {
                ClassHookContext.Current = null;
            }
        }));
    }

    internal async Task ExecuteBeforeHooks(ExecuteRequestContext executeRequestContext, ClassHookContext context)
    {
        foreach (var setUp in TestDictionary.GlobalClassSetUps.OrderBy(x => x.HookMethod.Order))
        {
            await setUp.Action.Value(executeRequestContext, hookMessagePublisher);
        }
    }

    internal async Task ExecuteAfterHooks(ExecuteRequestContext executeRequestContext, ClassHookContext context, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in TestDictionary.GlobalClassCleanUps.OrderBy(x => x.HookMethod.Order))
        {
            await hookMessagePublisher.Push(executeRequestContext, $"After Class: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
        }
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        TestDictionary.GlobalAssemblySetUps.Add((staticMethod.Name, staticMethod, new LazyHook<ExecuteRequestContext, HookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
        {
            var timeout = staticMethod.Timeout;
            var assemblyHookContext = AssemblyHookOrchestrator.GetAssemblyHookContext(staticMethod.Assembly);

            try
            {
                AssemblyHookContext.Current = assemblyHookContext;
                
                await hookPublisher.Push(executeRequestContext, $"Before Assembly: {staticMethod.Name}",
                    staticMethod, () =>
                        RunHelpers.RunWithTimeoutAsync(
                            token => staticMethod.HookExecutor.ExecuteBeforeAssemblyHook(staticMethod.MethodInfo,
                                assemblyHookContext,
                                () => staticMethod.Body(assemblyHookContext, token)), timeout)
                );
            }
            catch (Exception e)
            {
                throw new BeforeAssemblyException($"Error executing Before(Assembly) method: {staticMethod.Name}",
                    e);
            }
            finally
            {
                AssemblyHookContext.Current = null;
            }
        })));
    }

    public static void RegisterAfterHook(StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        TestDictionary.GlobalAssemblyCleanUps.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                AssemblyHookContext.Current = context;

                await RunHelpers.RunWithTimeoutAsync(
                    token => staticMethod.HookExecutor.ExecuteAfterAssemblyHook(staticMethod.MethodInfo, context,
                        () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterAssemblyException($"Error executing After(Assembly) method: {staticMethod.Name}", e);
            }
            finally
            {
                AssemblyHookContext.Current = null;
            }
        }));
    }

    internal async Task ExecuteBeforeHooks(ExecuteRequestContext executeRequestContext, AssemblyHookContext context)
    {
        foreach (var setUp in TestDictionary.GlobalAssemblySetUps.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                AssemblyHookContext.Current = context;
                
                await setUp.Action.Value(executeRequestContext, hookMessagePublisher);
            }
            finally
            {
                AssemblyHookContext.Current = null;
            }
        }
    }

    internal async Task ExecuteAfterHooks(ExecuteRequestContext executeRequestContext, AssemblyHookContext context,
        List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in TestDictionary.GlobalAssemblyCleanUps.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                AssemblyHookContext.Current = context;
                
                await hookMessagePublisher.Push(executeRequestContext, $"After Assembly: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
            }
            finally
            {
                AssemblyHookContext.Current = null;
            }
        }
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<BeforeTestDiscoveryContext> staticMethod)
    {
        TestDictionary.BeforeTestDiscovery.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                BeforeTestDiscoveryContext.Current = context;
                
                await RunHelpers.RunWithTimeoutAsync(
                    token => staticMethod.HookExecutor.ExecuteBeforeTestDiscoveryHook(staticMethod.MethodInfo,
                        () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new BeforeTestDiscoveryException(
                    $"Error executing Before(TestDiscovery) method: {staticMethod.Name}", e);
            }
            finally
            {
                BeforeTestDiscoveryContext.Current = null;
            }
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<TestDiscoveryContext> staticMethod)
    {
        TestDictionary.AfterTestDiscovery.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                TestDiscoveryContext.Current = context;
                
                await RunHelpers.RunWithTimeoutAsync(
                    token => staticMethod.HookExecutor.ExecuteAfterTestDiscoveryHook(staticMethod.MethodInfo, context,
                        () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterTestDiscoveryException(
                    $"Error executing After(TestDiscovery) method: {staticMethod.Name}", e);
            }
            finally
            {
                TestDiscoveryContext.Current = null;
            }
        }));
    }

    public static async Task ExecuteBeforeHooks(BeforeTestDiscoveryContext context)
    {
        foreach (var setUp in TestDictionary.BeforeTestDiscovery.OrderBy(x => x.HookMethod.Order))
        {
            BeforeTestDiscoveryContext.Current = context;

            try
            {
                await setUp.Action(context);
            }
            finally
            {
                BeforeTestDiscoveryContext.Current = null;
            }
        }
    }

    public static async Task ExecuteAfterHooks(TestDiscoveryContext context)
    {
        List<Exception> exceptions = []; 
        
        foreach (var cleanUp in TestDictionary.AfterTestDiscovery.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                TestDiscoveryContext.Current = context;

                await RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), exceptions);
            }
            finally
            {
                TestDiscoveryContext.Current = null;
            }
        }
        
        ExceptionsHelper.ThrowIfAny(exceptions);
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<TestSessionContext> staticMethod)
    {
        TestDictionary.BeforeTestSession.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                await RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeTestSessionHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new BeforeTestSessionException($"Error executing Before(TestSession) method: {staticMethod.Name}", e);
            }
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<TestSessionContext> staticMethod)
    {
        TestDictionary.AfterTestSession.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                await RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterTestSessionHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterTestSessionException($"Error executing After(TestSession) method: {staticMethod.Name}", e);
            }
        }));
    }

    public static async Task ExecuteBeforeHooks(TestSessionContext context)
    {
        foreach (var setUp in TestDictionary.BeforeTestSession.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                TestSessionContext.Current = context;

                await setUp.Action(context);
            }
            finally
            {
                TestSessionContext.Current = null;
            }
        }
    }

    public static async Task ExecuteAfterHooks(TestSessionContext context)
    {
        List<Exception> exceptions = []; 

        foreach (var cleanUp in TestDictionary.AfterTestSession.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                TestSessionContext.Current = context;
                
                await RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), exceptions);
            }
            finally
            {
                TestSessionContext.Current = null;
            }
        }
        
        ExceptionsHelper.ThrowIfAny(exceptions);
    }
}