using TUnit.Core;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class TestHookOrchestrator(HooksCollector hooksCollector)
{
    internal async Task ExecuteBeforeHooks(object classInstance, DiscoveredTest discoveredTest)
    {
        // Run instance ones first as they might initialize some variables etc.
        
        var testClassType = classInstance.GetType();
        
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType).Reverse();

        foreach (var type in typesIncludingBase)
        {
            var list = hooksCollector.BeforeTestHooks.GetOrAdd(type, _ => []);
            
            foreach (var instanceHookMethod in list)
            {
                await Timings.Record($"Test Hook Set Up: {instanceHookMethod.Name}", discoveredTest.TestContext, () =>
                    instanceHookMethod.HookExecutor.ExecuteBeforeTestHook(
                        hookMethodInfo: instanceHookMethod.MethodInfo,
                        context: discoveredTest.TestContext,
                        action: () => instanceHookMethod.ExecuteHook(discoveredTest.TestContext, default)
                    )
                );
            }
        }
        
        foreach (var beforeEvery in hooksCollector.BeforeEveryTestHooks)
        {
            await beforeEvery.HookExecutor.ExecuteBeforeTestHook(
                hookMethodInfo: beforeEvery.MethodInfo,
                context: discoveredTest.TestContext,
                action: () => beforeEvery.Body(discoveredTest.TestContext, default)
            );
        }
    }
    
    internal async Task ExecuteAfterHooks(object classInstance, DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions)
    {
        var testClassType = classInstance.GetType();
        
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            var list = hooksCollector.AfterTestHooks.GetOrAdd(type, _ => []);
            
            foreach (var instanceHookMethod in list)
            {
                await Timings.Record($"Test Hook Set Up: {instanceHookMethod.Name}", discoveredTest.TestContext, () =>
                    RunHelpers.RunSafelyAsync(() =>
                            instanceHookMethod.HookExecutor.ExecuteBeforeTestHook(
                                hookMethodInfo: instanceHookMethod.MethodInfo,
                                context: discoveredTest.TestContext,
                                action: () => instanceHookMethod.ExecuteHook(discoveredTest.TestContext, default)
                            ),
                        cleanUpExceptions
                    )
                );
            }
        }
        
        // Run Global Hooks Last

        foreach (var afterEvery in hooksCollector.AfterEveryTestHooks)
        {
            await RunHelpers.RunSafelyAsync(() =>
                    afterEvery.HookExecutor.ExecuteBeforeTestHook(
                        hookMethodInfo: afterEvery.MethodInfo,
                        context: discoveredTest.TestContext,
                        action: () => afterEvery.Body(discoveredTest.TestContext, default)
                    ),
                cleanUpExceptions
            );
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