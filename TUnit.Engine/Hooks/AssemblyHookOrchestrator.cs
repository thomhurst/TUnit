using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class AssemblyHookOrchestrator
{
    private static readonly List<Lazy<Task>> SetUps = [];
    private static readonly List<Lazy<Task>> CleanUps = [];

    public static void RegisterSetUp(StaticMethod staticMethod)
    {
        SetUps.Add(Convert(staticMethod));
    }

    public static void RegisterCleanUp(StaticMethod staticMethod)
    {
        CleanUps.Add(Convert(staticMethod));
    }

    private static Lazy<Task> Convert(StaticMethod staticMethod)
    {
        return new Lazy<Task>(() =>
        {
            var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(EngineCancellationToken.CancellationTokenSource.Token);
            var timeout = staticMethod.MethodInfo.GetTimeout();

            if (timeout != null)
            {
                cancellationToken.CancelAfter(timeout.Value);
            }
            
            return staticMethod.Body(cancellationToken.Token);
        });
    }

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