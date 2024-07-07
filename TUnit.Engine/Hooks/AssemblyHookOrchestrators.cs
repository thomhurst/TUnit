namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class AssemblyHookOrchestrators
{
    private static readonly List<Lazy<Task>> SetUps = [];
    private static readonly List<Lazy<Task>> CleanUps = [];

    public static void RegisterSetUp(Func<Task> func) => SetUps.Add(new Lazy<Task>(func));
    public static void RegisterCleanUp(Func<Task> func) => CleanUps.Add(new Lazy<Task>(func));

    public static async Task ExecuteSetups()
    {
        foreach (var setUp in SetUps)
        {
            await setUp.Value;
        }
    }

    public static async Task ExecuteCleanups()
    {
        var exceptions = new List<Exception>();
        
        foreach (var cleanUp in CleanUps)
        {
            try
            {
                await cleanUp.Value;
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