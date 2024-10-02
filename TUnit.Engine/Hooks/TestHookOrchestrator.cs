using TUnit.Core;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestHookOrchestrator
{
    internal static async Task ExecuteBeforeHooks(object classInstance, DiscoveredTest discoveredTest)
    {
        // Run Global Hooks First
        await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(discoveredTest);

        var testClassType = classInstance.GetType();
        
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType).Reverse();

        foreach (var type in typesIncludingBase)
        {
            if (!TestDictionary.TestSetUps.TryGetValue(type, out var setUpsForType))
            {
                continue;
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
            if (!TestDictionary.TestCleanUps.TryGetValue(type, out var cleanUpsForType))
            {
                continue;
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