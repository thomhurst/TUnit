using System.Runtime.CompilerServices;

namespace TUnit.Engine;

public static class GlobalTestHookOrchestrator
{
    private static readonly List<Func<Task>> SetUps = new();
    private static readonly List<Func<Task>> CleanUps = new();

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterSetUp(Func<Task> taskFactory)
    {
        SetUps.Add(taskFactory);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterCleanUp(Func<Task> taskFactory)
    {
        CleanUps.Add(taskFactory);
    }

    public static async Task ExecuteSetups()
    {
        foreach (var setUp in SetUps)
        {
            await setUp();
        }
    }

    public static async Task ExecuteCleanUps(List<Exception> cleanUpExceptions)
    {
        foreach (var cleanUp in CleanUps)
        {
            try
            {
                await cleanUp();
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