using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.Engine.Hooks;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class GlobalTestHookOrchestrator
{
    private static readonly List<Func<TestContext, Task>> SetUps = [];
    private static readonly List<Func<TestContext, Task>> CleanUps = [];

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterSetUp(Func<TestContext, Task> taskFactory)
    {
        SetUps.Add(taskFactory);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterCleanUp(Func<TestContext, Task> taskFactory)
    {
        CleanUps.Add(taskFactory);
    }

    public static async Task ExecuteSetups(TestContext testContext)
    {
        foreach (var setUp in SetUps)
        {
            await setUp(testContext);
        }
    }

    public static async Task ExecuteCleanUps(TestContext testContext, List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in CleanUps)
        {
            try
            {
                await cleanUp(testContext);
            }
            catch (Exception e)
            {
                cleanUpExceptions.Add(e);
            }
        }

        if (cleanUpExceptions.Count == 1)
        {
            throw cleanUpExceptions[0];
        }

        if (cleanUpExceptions.Count > 1)
        {
            throw new AggregateException(cleanUpExceptions);
        }
    }
}