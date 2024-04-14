using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace TUnit.Engine;

public static class ClassHookOrchestrator
{
    private static readonly ConcurrentDictionary<Type, List<Lazy<Task>>> SetUps = new();
    private static readonly ConcurrentDictionary<Type, List<Lazy<Task>>> CleanUps = new();

    private static readonly ConcurrentDictionary<Type, int> InstanceTrackers = new();

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterInstance(Type type)
    {
        var count = InstanceTrackers.GetOrAdd(type, _ => 0);
        InstanceTrackers[type] = count + 1;
    }
    
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterSetUp(Type type, Func<Task> taskFactory)
    {
        var taskFunctions = SetUps.GetOrAdd(type, _ => []);
        
        taskFunctions.Add(new Lazy<Task>(taskFactory));
    }
    
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterCleanUp(Type type, Func<Task> taskFactory)
    {
        var taskFunctions = CleanUps.GetOrAdd(type, _ => []);
        
        taskFunctions.Add(new Lazy<Task>(taskFactory));
    }
    
    public static async Task ExecuteSetups(Type testClassType)
    {
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType)
            .Reverse();

        foreach (var type in typesIncludingBase)
        {
            if (!SetUps.TryGetValue(type, out var setUpsForType))
            {
                return;
            }

            foreach (var setUp in setUpsForType)
            {
                // As these are lazy we should always get the same Task
                // So we await the same Task to ensure it's finished first
                // and also gives the benefit of rethrowing the same exception if it failed
                await setUp.Value;
            }
        }
    }
    
    public static async Task ExecuteCleanUpsIfLastInstance(Type testClassType, List<Exception> cleanUpExceptions)
    {
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            var instanceCount = DecreaseCount(type);

            if (instanceCount != 0)
            {
                // Only run one time clean down's when no instances are left!
                continue;
            }
            
            if (!CleanUps.TryGetValue(type, out var cleanUpsForType))
            {
                return;
            }

            foreach (var cleanUp in cleanUpsForType)
            {
                try
                {
                    await cleanUp.Value;
                }
                catch (Exception e)
                {
                    cleanUpExceptions.Add(e);
                }
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

    private static IEnumerable<Type> GetTypesIncludingBase(Type testClassType)
    {
        var type = testClassType;
        
        while (type != null)
        {
            yield return type;
            type = type.BaseType;
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static int DecreaseCount(Type type)
    {
        var count = InstanceTrackers[type];
        
        var newCount = count - 1;
        
        InstanceTrackers[type] = newCount;

        return newCount;
    }
}