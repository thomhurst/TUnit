using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestHookOrchestrator
{
    private static readonly ConcurrentDictionary<Type, List<Func<object, TestContext, Task>>> SetUps = new();
    private static readonly ConcurrentDictionary<Type, List<Func<object, TestContext, Task>>> CleanUps = new();
    
    public static void RegisterSetUp<TClassType>(InstanceMethod<TClassType> instanceMethod)
    {
        var taskFunctions = SetUps.GetOrAdd(typeof(TClassType), _ => []);

        taskFunctions.Add((classInstance, testContext) =>
        {
            var timeout = instanceMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => instanceMethod.Body((TClassType) classInstance, testContext, token), timeout);
        });
    }
    
    public static void RegisterCleanUp<TClassType>(InstanceMethod<TClassType> instanceMethod)
    {
        var taskFunctions = CleanUps.GetOrAdd(typeof(TClassType), _ => []);

        taskFunctions.Add((classInstance, testContext) =>
        {
            var timeout = instanceMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => instanceMethod.Body((TClassType) classInstance, testContext, token), timeout);
        });
    }
    
    public static async Task ExecuteSetups(object classInstance, TestContext testContext)
    {
        var testClassType = classInstance.GetType();
        
        await AssemblyHookOrchestrator.ExecuteSetups(testClassType.Assembly);

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
                await setUp(classInstance, testContext);
            }
        }
    }
    
    public static async Task ExecuteCleanUps(object classInstance, TestContext testContext, List<Exception> cleanUpExceptions)
    {
        var testClassType = classInstance.GetType();
        
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            if (!CleanUps.TryGetValue(type, out var cleanUpsForType))
            {
                return;
            }

            foreach (var cleanUp in cleanUpsForType)
            {
                await RunHelpers.RunSafelyAsync(() => cleanUp(classInstance, testContext), cleanUpExceptions);
            }
        }
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