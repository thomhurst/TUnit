using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TUnit.Engine.Data;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class AssemblyHookOrchestrator
{
    private static readonly GetOnlyDictionary<Assembly, List<Lazy<Task>>> SetUps = new();
    private static readonly GetOnlyDictionary<Assembly, List<Lazy<Task>>> CleanUps = new();

    private static readonly object Lock = new();
    private static readonly ConcurrentDictionary<Assembly, int> CountTracker = new(); 

    public static void RegisterSetUp(StaticMethod staticMethod)
    {
        var setups = SetUps.GetOrAdd(staticMethod.MethodInfo.ReflectedType!.Assembly, _ => []);
        setups.Add(Convert(staticMethod));
    }

    public static void RegisterCleanUp(StaticMethod staticMethod)
    {
        var cleanups = CleanUps.GetOrAdd(staticMethod.MethodInfo.ReflectedType!.Assembly, _ => []);
        cleanups.Add(Convert(staticMethod));
    }

    private static Lazy<Task> Convert(StaticMethod staticMethod)
    {
        return new Lazy<Task>(() =>
        {
            var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(EngineCancellationToken.Token);
            var timeout = staticMethod.Timeout;

            if (timeout != null)
            {
                cancellationToken.CancelAfter(timeout.Value);
            }
            
            return staticMethod.Body(cancellationToken.Token);
        });
    }

    public static async Task ExecuteSetups(Assembly assembly)
    {
        foreach (var setUp in SetUps.GetOrAdd(assembly, _ => []))
        {
            await setUp.Value;
        }
    }

    public static async Task ExecuteCleanups(Assembly assembly, List<Exception> exceptions)
    {
        lock (Lock)
        {
            var count = CountTracker.GetOrAdd(assembly, _ => 0);
            var newCount = count - 1;
            CountTracker[assembly] = newCount;

            if (newCount > 0)
            {
                return;
            }
        }
        
        foreach (var cleanUp in CleanUps.GetOrAdd(assembly, _ => []))
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
    }

    public static void Increment(Assembly assembly)
    {
        var count = CountTracker.GetOrAdd(assembly, _ => 0);
        CountTracker[assembly] = count + 1;
    }
}