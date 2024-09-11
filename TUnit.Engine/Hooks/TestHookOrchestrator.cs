using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestHookOrchestrator
{
    private static readonly ConcurrentDictionary<Type, List<(string Name, int Order, Func<object, DiscoveredTest, Task> Action)>> SetUps = new();
    private static readonly ConcurrentDictionary<Type, List<(string Name, int Order, Func<object, DiscoveredTest, Task> Action)>> CleanUps = new();
    
    public static void RegisterBeforeHook<TClassType>(InstanceHookMethod<TClassType> instanceMethod)
    {
        var taskFunctions = SetUps.GetOrAdd(typeof(TClassType), _ => []);

        taskFunctions.Add((instanceMethod.Name, instanceMethod.Order, async (classInstance, discoveredTest) =>
        {
            var timeout = instanceMethod.Timeout;

            try
            {
                await RunHelpers.RunWithTimeoutAsync(token => HookExecutorProvider.GetHookExecutor(instanceMethod, discoveredTest).ExecuteBeforeTestHook(instanceMethod.MethodInfo, discoveredTest.TestContext, () => instanceMethod.Body((TClassType) classInstance, discoveredTest.TestContext, token)), timeout);
            }
            catch (Exception e)
            {
                throw new BeforeTestException($"Error executing Before(Test) method: {instanceMethod.Name}", e);
            }
        }));
    }
    
    public static void RegisterAfterHook<TClassType>(InstanceHookMethod<TClassType> instanceMethod)
    {
        var taskFunctions = CleanUps.GetOrAdd(typeof(TClassType), _ => []);

        taskFunctions.Add((instanceMethod.Name, instanceMethod.Order, async (classInstance, discoveredTest) =>
        {
            var timeout = instanceMethod.Timeout;

            try
            {
                await RunHelpers.RunWithTimeoutAsync(token => HookExecutorProvider.GetHookExecutor(instanceMethod, discoveredTest).ExecuteAfterTestHook(instanceMethod.MethodInfo, discoveredTest.TestContext, () => instanceMethod.Body((TClassType) classInstance, discoveredTest.TestContext, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterTestException($"Error executing After(Test) method: {instanceMethod.Name}", e);
            }
        }));
    }

    internal static async Task ExecuteBeforeHooks(object classInstance, DiscoveredTest discoveredTest)
    {
        // Run Global Hooks First
        await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(discoveredTest);

        var testClassType = classInstance.GetType();
        
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType).Reverse();

        foreach (var type in typesIncludingBase)
        {
            if (!SetUps.TryGetValue(type, out var setUpsForType))
            {
                return;
            }

            foreach (var setUp in setUpsForType.OrderBy(x => x.Order))
            {
                await Timings.Record($"Test Hook Set Up: {setUp.Name}", discoveredTest.TestContext, () => setUp.Action(classInstance, discoveredTest));
            }
        }
    }
    
    internal static async Task ExecuteAfterHooks(object classInstance, DiscoveredTest testContext, List<Exception> cleanUpExceptions)
    {
        var testClassType = classInstance.GetType();
        
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            if (!CleanUps.TryGetValue(type, out var cleanUpsForType))
            {
                return;
            }

            foreach (var cleanUp in cleanUpsForType.OrderBy(x => x.Order))
            {
                await Timings.Record($"Test Hook Clean Up: {cleanUp.Name}", testContext.TestContext, () => RunHelpers.RunSafelyAsync(() => cleanUp.Action(classInstance, testContext),
                    cleanUpExceptions));
            }
        }
        
        // Run Global Hooks Last
        await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(testContext, cleanUpExceptions);
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
}