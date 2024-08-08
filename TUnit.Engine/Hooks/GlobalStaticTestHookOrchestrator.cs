using TUnit.Core;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class GlobalStaticTestHookOrchestrator
{
    private static readonly List<(string Name, Func<TestContext, Task> Action)> SetUps = [];
    private static readonly List<(string Name, Func<TestContext, Task> Action)> CleanUps = [];

    private static readonly List<(string Name, Func<ClassHookContext, Task> Action)> ClassSetUps = [];
    private static readonly List<(string Name, Func<ClassHookContext, Task> Action)> ClassCleanUps = [];
    
    private static readonly List<(string Name, Func<AssemblyHookContext, Task> Action)> AssemblySetUps = [];
    private static readonly List<(string Name, Func<AssemblyHookContext, Task> Action)> AssemblyCleanUps = [];
    
    public static void RegisterSetUp(StaticMethod<TestContext> staticMethod)
    {
        SetUps.Add((staticMethod.Name, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(context, token), timeout);
        }));
    }

    public static void RegisterCleanUp(StaticMethod<TestContext> staticMethod)
    {
        CleanUps.Add((staticMethod.Name, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(context, token), timeout);
        }));
    }

    public static async Task ExecuteSetups(TestContext context)
    {
        foreach (var setUp in SetUps)
        {
            await Timings.Record("Global Static Test Hook Set Up: " + setUp.Name, context, 
                () => setUp.Action(context));
        }
    }

    public static async Task ExecuteCleanUps(TestContext context, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in CleanUps)
        {
            await Timings.Record("Global Static Test Hook Clean Up: " + cleanUp.Name, context,
                () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
        }
    }
    
    public static void RegisterSetUp(StaticMethod<ClassHookContext> staticMethod)
    {
        ClassSetUps.Add((staticMethod.Name, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(context, token), timeout);
        }));
    }

    public static void RegisterCleanUp(StaticMethod<ClassHookContext> staticMethod)
    {
        ClassCleanUps.Add((staticMethod.Name, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(context, token), timeout);
        }));
    }

    public static async Task ExecuteSetups(ClassHookContext context)
    {
        foreach (var setUp in ClassSetUps)
        {
            await Timings.Record("Global Static Class Hook Set Up: " + setUp.Name, context.Tests.MinBy(x => x.Timings.MinBy(t => t.Start)!.Start)!, 
                () => setUp.Action(context));
        }
    }

    public static async Task ExecuteCleanUps(ClassHookContext context, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in ClassCleanUps)
        {
            await Timings.Record("Global Static Class Hook Clean Up: " + cleanUp.Name, context.Tests.MaxBy(x => x.Timings.MaxBy(t => t.End)!.End)!,
                () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
        }
    }
    
    public static void RegisterSetUp(StaticMethod<AssemblyHookContext> staticMethod)
    {
        AssemblySetUps.Add((staticMethod.Name, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(context, token), timeout);
        }));
    }

    public static void RegisterCleanUp(StaticMethod<AssemblyHookContext> staticMethod)
    {
        AssemblyCleanUps.Add((staticMethod.Name, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(context, token), timeout);
        }));
    }

    public static async Task ExecuteSetups(AssemblyHookContext context, TestContext initiatingTestContext)
    {
        foreach (var setUp in AssemblySetUps)
        {
            await Timings.Record("Global Static Assembly Hook Set Up: " + setUp.Name, initiatingTestContext, 
                () => setUp.Action(context));
        }
    }

    public static async Task ExecuteCleanUps(AssemblyHookContext context, TestContext initiatingTestContext,
        List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in AssemblyCleanUps)
        {
            await Timings.Record("Global Static Assembly Hook Clean Up: " + cleanUp.Name, initiatingTestContext,
                () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(context), cleanUpExceptions));
        }
    }
}