namespace TUnit.Engine;

public static class AssemblyHookOrchestrators
{
    private static readonly List<Func<Task>> SetUps = new();
    private static readonly List<Func<Task>> CleanUps = new();

    public static void RegisterSetUp(Func<Task> func) => SetUps.Add(func);
    public static void RegisterCleanUp(Func<Task> func) => SetUps.Add(func);

    public static async Task ExecuteSetups()
    {
        foreach (var setUp in SetUps)
        {
            await setUp();
        }
    }

    public static async Task ExecuteCleanups()
    {
        var exceptions = new List<Exception>();
        
        foreach (var setUp in CleanUps)
        {
            try
            {
                await setUp();
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        if (exceptions.Count == 1)
        {
            throw exceptions[0];
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException(exceptions);
        }
    }
}