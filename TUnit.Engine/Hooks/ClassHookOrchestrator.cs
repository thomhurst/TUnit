using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class ClassHookOrchestrator
{
    private static readonly ConcurrentDictionary<Type, List<(string Name, int Order, Lazy<Task> Action)>> SetUps = new();
    private static readonly ConcurrentDictionary<Type, List<(string Name, int Order, Func<Task> Action)>> CleanUps = new();
    
    private static readonly ConcurrentDictionary<Type, ClassHookContext> ClassHookContexts = new();
    
    public static void RegisterBeforeHook(Type type, StaticHookMethod<ClassHookContext> staticMethod)
    {
        var taskFunctions = SetUps.GetOrAdd(type, _ => []);

        taskFunctions.Add((staticMethod.Name, staticMethod.Order, Convert(type, staticMethod)));
    }
    
    public static void RegisterAfterHook(Type type, StaticHookMethod<ClassHookContext> staticMethod)
    {
        var taskFunctions = CleanUps.GetOrAdd(type, _ => []);

        taskFunctions.Add((staticMethod.Name, staticMethod.Order, () =>
        {
            var context = GetClassHookContext(type);
            
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterClassHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
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

    private static ClassHookContext GetClassHookContext(Type type)
    {
        lock (type)
        {
            return ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
            {
                ClassType = type
            });
        }
    }
    
    public static async Task ExecuteBeforeHooks(Type testClassType, TestContext testContext)
    {
        var context = GetClassHookContext(testClassType);
        
        // Run Global Hooks First
        await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(context, testContext.InternalDiscoveredTest);
            
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType)
            .Reverse();

        foreach (var type in typesIncludingBase)
        {
            if (!SetUps.TryGetValue(type, out var setUpsForType))
            {
                return;
            }

            foreach (var setUp in setUpsForType.OrderBy(x => x.Order))
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

            foreach (var cleanUp in cleanUpsForType.OrderBy(x => x.Order))
            {
                await Timings.Record("Class Hook Clean Up: " + cleanUp.Name, testContext, () => RunHelpers.RunSafelyAsync(cleanUp.Action, cleanUpExceptions));
            }
            
            var context = GetClassHookContext(testClassType);
        
            // Run Global Hooks Last
            await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(context, testContext.InternalDiscoveredTest, cleanUpExceptions);
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
    
    private static Lazy<Task> Convert(Type type, StaticHookMethod<ClassHookContext> staticMethod)
    {
        return new Lazy<Task>(() =>
        {
            var context = GetClassHookContext(type);
            
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeClassHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        });
    }
}