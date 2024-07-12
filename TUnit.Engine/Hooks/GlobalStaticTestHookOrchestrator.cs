using System.Runtime.CompilerServices;
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

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterSetUp(StaticMethod<TestContext> staticMethod)
    {
        SetUps.Add((staticMethod.Name, context =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(context, token), timeout);
        }));
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterCleanUp(StaticMethod<TestContext> staticMethod)
    {
        CleanUps.Add((staticMethod.Name, context =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(context, token), timeout);
        }));
    }

    public static async Task ExecuteSetups(TestContext testContext)
    {
        foreach (var setUp in SetUps)
        {
            await Timings.Record("Global Static Test Hook Set Up: " + setUp.Name, testContext, 
                () => setUp.Action(testContext));
        }
    }

    public static async Task ExecuteCleanUps(TestContext testContext, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in CleanUps)
        {
            await Timings.Record("Global Static Test Hook Set Up: " + cleanUp.Name, testContext,
                () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(testContext), cleanUpExceptions));
        }
    }
}