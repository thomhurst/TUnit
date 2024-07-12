using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Models;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class ClassHookOrchestrator
{
    private static readonly ConcurrentDictionary<Type, List<(string Name, Lazy<Task> Action)>> SetUps = new();
    private static readonly ConcurrentDictionary<Type, List<(string Name, Func<Task> Action)>> CleanUps = new();
    
    private static readonly ConcurrentDictionary<Type, ClassHookContext> ClassHookContexts = new();
    
    public static void RegisterSetUp(Type type, StaticMethod staticMethod)
    {
        var taskFunctions = SetUps.GetOrAdd(type, _ => []);

        taskFunctions.Add((staticMethod.Name, Convert(staticMethod)));
    }
    
    public static void RegisterCleanUp(Type type, StaticMethod staticMethod)
    {
        var taskFunctions = CleanUps.GetOrAdd(type, _ => []);

        taskFunctions.Add((staticMethod.Name, () =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(token), timeout);
        }));
    }
    
    public static void RegisterTestContext(Type type, TestContext testContext)
    {
        var classHookContext = ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
            {
                ClassType = type
            });

        classHookContext.Tests.Add(testContext);
        
        AssemblyHookOrchestrator.RegisterTestContext(type.Assembly, classHookContext);
    }
    
    public static ClassHookContext GetClassHookContext(Type type)
    {
        lock (type)
        {
            return ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
            {
                ClassType = type
            });
        }
    }
    
    public static async Task ExecuteSetups(Type testClassType, TestContext testContext)
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
                await Timings.Record("Class Hook Set Up: " + setUp.Name, testContext, () => setUp.Action.Value);
            }
        }
    }
    
    public static async Task ExecuteCleanUpsIfLastInstance(Type testClassType,
        TestContext testContext,
        List<Exception> cleanUpExceptions)
    {
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            if (!InstanceTracker.IsLastTestForType(type))
            {
                // Only run one time clean down's when no instances are left!
                continue;
            }

            await TestDataContainer.OnLastInstance(testClassType);
            
            if (!CleanUps.TryGetValue(type, out var cleanUpsForType))
            {
                return;
            }

            foreach (var cleanUp in cleanUpsForType)
            {
                await Timings.Record("Class Hook Clean Up: " + cleanUp.Name, testContext, () => RunHelpers.RunSafelyAsync(cleanUp.Action, cleanUpExceptions));
            }
        }
    }

    public static IEnumerable<TestContext> GetTestsForType(Type type)
    {
        var context = ClassHookContexts.GetOrAdd(type, new ClassHookContext
        {
            ClassType = type
        });

        return context.Tests;
    }

    private static IEnumerable<Type> GetTypesIncludingBase(Type testClassType)
    {
        var type = testClassType;
        
        while (type != null && type != typeof(object))
        {
            yield return type;
            type = type.BaseType;
        }
    }
    
    private static Lazy<Task> Convert(StaticMethod staticMethod)
    {
        return new Lazy<Task>(() =>
        {
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(token), timeout);
        });
    }
}