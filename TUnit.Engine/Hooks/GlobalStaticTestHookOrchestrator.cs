using TUnit.Core;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class GlobalStaticTestHookOrchestrator
{
    private static readonly List<(string Name, int Order, Func<TestContext, Task> Action)> SetUps = [];
    private static readonly List<(string Name, int Order, Func<TestContext, Task> Action)> CleanUps = [];

    private static readonly List<(string Name, int Order, Func<ClassHookContext, Task> Action)> ClassSetUps = [];
    private static readonly List<(string Name, int Order, Func<ClassHookContext, Task> Action)> ClassCleanUps = [];
    
    private static readonly List<(string Name, int Order, Func<AssemblyHookContext, Task> Action)> AssemblySetUps = [];
    private static readonly List<(string Name, int Order, Func<AssemblyHookContext, Task> Action)> AssemblyCleanUps = [];
    
    private static readonly List<(string Name, int Order, Func<BeforeTestDiscoveryContext, Task> Action)> BeforeTestDiscovery = [];
    private static readonly List<(string Name, int Order, Func<TestDiscoveryContext, Task> Action)> AfterTestDiscovery = [];
    
    private static readonly List<(string Name, int Order, Func<TestSessionContext, Task> Action)> BeforeTestSession = [];
    private static readonly List<(string Name, int Order, Func<TestSessionContext, Task> Action)> AfterTestSession = [];
    
    public static void RegisterBeforeHook(StaticHookMethod<TestContext> staticMethod)
    {
        SetUps.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeTestHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<TestContext> staticMethod)
    {
        CleanUps.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterTestHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static async Task ExecuteBeforeHooks(TestContext context)
    {
        foreach (var setUp in SetUps.OrderBy(x => x.Order))
        {
            await Timings.Record("Global Static Test Hook Set Up: " + setUp.Name, context, 
                () => setUp.Action(context));
        }
    }

    public static async Task ExecuteAfterHooks(TestContext context, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in CleanUps.OrderBy(x => x.Order))
        {
            await Timings.Record("Global Static Test Hook Clean Up: " + cleanUp.Name, context,
                () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
        }
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<ClassHookContext> staticMethod)
    {
        ClassSetUps.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeClassHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<ClassHookContext> staticMethod)
    {
        ClassCleanUps.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterClassHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static async Task ExecuteBeforeHooks(ClassHookContext context, TestContext initiatingTestContext)
    {
        foreach (var setUp in ClassSetUps.OrderBy(x => x.Order))
        {
            await Timings.Record("Global Static Class Hook Set Up: " + setUp.Name, initiatingTestContext, 
                () => setUp.Action(context));
        }
    }

    public static async Task ExecuteAfterHooks(ClassHookContext context, TestContext initiatingTestContext, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in ClassCleanUps.OrderBy(x => x.Order))
        {
            await Timings.Record("Global Static Class Hook Clean Up: " + cleanUp.Name, initiatingTestContext,
                () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
        }
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        AssemblySetUps.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeAssemblyHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        AssemblyCleanUps.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterAssemblyHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static async Task ExecuteBeforeHooks(AssemblyHookContext context, TestContext initiatingTestContext)
    {
        foreach (var setUp in AssemblySetUps.OrderBy(x => x.Order))
        {
            await Timings.Record("Global Static Assembly Hook Set Up: " + setUp.Name, initiatingTestContext, 
                () => setUp.Action(context));
        }
    }

    public static async Task ExecuteAfterHooks(AssemblyHookContext context, TestContext initiatingTestContext,
        List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in AssemblyCleanUps.OrderBy(x => x.Order))
        {
            await Timings.Record("Global Static Assembly Hook Clean Up: " + cleanUp.Name, initiatingTestContext,
                () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
        }
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<BeforeTestDiscoveryContext> staticMethod)
    {
        BeforeTestDiscovery.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeTestDiscoveryHook(staticMethod.MethodInfo, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<TestDiscoveryContext> staticMethod)
    {
        AfterTestDiscovery.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterTestDiscoveryHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static async Task ExecuteBeforeHooks(BeforeTestDiscoveryContext context)
    {
        foreach (var setUp in BeforeTestDiscovery.OrderBy(x => x.Order))
        {
            await setUp.Action(context);
        }
    }

    public static async Task ExecuteAfterHooks(TestDiscoveryContext context)
    {
        List<Exception> exceptions = []; 
        
        foreach (var cleanUp in AfterTestDiscovery.OrderBy(x => x.Order))
        {
            await RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), exceptions);
        }
        
        ExceptionsHelper.ThrowIfAny(exceptions);
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<TestSessionContext> staticMethod)
    {
        BeforeTestSession.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeTestSessionHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<TestSessionContext> staticMethod)
    {
        AfterTestSession.Add((staticMethod.Name, staticMethod.Order, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterTestSessionHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }

    public static async Task ExecuteBeforeHooks(TestSessionContext context)
    {
        foreach (var setUp in BeforeTestSession.OrderBy(x => x.Order))
        {
            await setUp.Action(context);
        }
    }

    public static async Task ExecuteAfterHooks(TestSessionContext context)
    {
        List<Exception> exceptions = []; 

        foreach (var cleanUp in AfterTestSession.OrderBy(x => x.Order))
        {
            await RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), exceptions);
        }
        
        ExceptionsHelper.ThrowIfAny(exceptions);
    }
}