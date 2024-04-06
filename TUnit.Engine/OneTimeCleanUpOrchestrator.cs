using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TUnit.Engine;

public static class OneTimeCleanUpOrchestrator
{
    private static readonly ConcurrentDictionary<Type, int> RemainingTests = new();
    private static readonly ConcurrentDictionary<Type, List<Func<Task>>> OneTimeCleanUps = new();
    
    private static readonly SemaphoreSlim NotifyLock = new(1, 1);
    
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterTest(Type testClassType)
    {
        var type = testClassType;

        while (type != null)
        {
            var count = RemainingTests.GetOrAdd(type, 0);
            RemainingTests[testClassType] = count + 1;
            type = type.BaseType;
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterOneTimeTearDown(Type testClassType, Func<Task> func)
    {
        var list = OneTimeCleanUps.GetOrAdd(testClassType, _ => new List<Func<Task>>());
        list.Add(func);
    }

    public static async Task NotifyCompletedTestAndRunOneTimeCleanUps(Type testClassType,
        List<Exception> teardownExceptions)
    {
        await NotifyLock.WaitAsync();

        try
        {
            var type = testClassType;

            while (type != null)
            {
                var count = RemainingTests[testClassType];

                var remainingTestCountForType = count - 1;

                RemainingTests[testClassType] = remainingTestCountForType;

                if (remainingTestCountForType == 0 
                    && OneTimeCleanUps.TryGetValue(type, out var oneTimeCleanUpDelegates))
                {
                    foreach (var oneTimeCleanUpDelegate in oneTimeCleanUpDelegates)
                    {
                        await RunHelpers.RunSafelyAsync(oneTimeCleanUpDelegate, teardownExceptions);
                    }
                }
            
                type = type.BaseType;
            }
        
        }
        finally
        {
            NotifyLock.Release();
        }
    }
}