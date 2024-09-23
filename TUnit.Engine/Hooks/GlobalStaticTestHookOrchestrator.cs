using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
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
            await hookMessagePublisher.Discover(context.Request.Session.SessionUid.Value, $"Before Class: {name}", hookMethod);
        }
        
        foreach (var (name, hookMethod, _) in TestDictionary.GlobalClassCleanUps)
        {
            await hookMessagePublisher.Discover(context.Request.Session.SessionUid.Value, $"After Class: {name}", hookMethod);
        }
        
        foreach (var (name, hookMethod, _) in TestDictionary.GlobalAssemblySetUps)
        {
            await hookMessagePublisher.Discover(context.Request.Session.SessionUid.Value, $"Before Assembly: {name}", hookMethod);
        }
        
        foreach (var (name, hookMethod, _) in TestDictionary.GlobalAssemblyCleanUps)
        {
            await hookMessagePublisher.Discover(context.Request.Session.SessionUid.Value, $"After Assembly: {name}", hookMethod);
        }
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
    
    internal async Task ExecuteBeforeHooks(ExecuteRequestContext executeRequestContext, ClassHookContext context)
    {
        foreach (var setUp in TestDictionary.GlobalClassSetUps.OrderBy(x => x.HookMethod.Order))
        {
            await setUp.Action.Value(executeRequestContext.Request.Session.SessionUid.Value, hookMessagePublisher);
        }
    }

    internal async Task ExecuteAfterHooks(ExecuteRequestContext executeRequestContext, ClassHookContext context, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in TestDictionary.GlobalClassCleanUps.OrderBy(x => x.HookMethod.Order))
        {
            await hookMessagePublisher.Push(executeRequestContext.Request.Session.SessionUid.Value, $"After Class: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
        }
    }

    internal async Task ExecuteBeforeHooks(ExecuteRequestContext executeRequestContext, AssemblyHookContext context)
    {
        foreach (var setUp in TestDictionary.GlobalAssemblySetUps.OrderBy(x => x.HookMethod.Order))
        {
            try
            {
                AssemblyHookContext.Current = context;
                
                await setUp.Action.Value(executeRequestContext.Request.Session.SessionUid.Value, hookMessagePublisher);
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
                
                await hookMessagePublisher.Push(executeRequestContext.Request.Session.SessionUid.Value, $"After Assembly: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
            }
            finally
            {
                AssemblyHookContext.Current = null;
            }
        }
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