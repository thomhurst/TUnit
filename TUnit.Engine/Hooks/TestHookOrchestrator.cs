using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class TestHookOrchestrator(HooksCollector hooksCollector)
{
    internal IEnumerable<IExecutableHook<TestContext>> CollectBeforeHooks(object classInstance, DiscoveredTest discoveredTest)
    {
        var testClassType = classInstance.GetType();
        
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType).Reverse();

        foreach (var type in typesIncludingBase)
        {
            foreach (var instanceHookMethod in hooksCollector.BeforeTestHooks.GetOrAdd(type, _ => [])
                         .OrderBy(x => x.Order)
                         .OfType<IExecutableHook<TestContext>>())
            {
                yield return instanceHookMethod;
            }
        }
        
        foreach (var beforeEvery in hooksCollector.BeforeEveryTestHooks
                     .OrderBy(x => x.Order)
                     .OfType<IExecutableHook<TestContext>>())
        {
            yield return beforeEvery;
        }
    }
    
    internal IEnumerable<IExecutableHook<TestContext>> CollectAfterHooks(object classInstance, DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions)
    {
        var testClassType = classInstance.GetType();
        
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            foreach (var instanceHookMethod in hooksCollector.AfterTestHooks.GetOrAdd(type, _ => [])
                         .OrderBy(x => x.Order)
                         .OfType<IExecutableHook<TestContext>>())
            {
                yield return instanceHookMethod;
            }
        }
        
        // Run Global Hooks Last
        foreach (var afterEvery in hooksCollector.AfterEveryTestHooks
                     .OrderBy(x => x.Order)
                     .OfType<IExecutableHook<TestContext>>())
        {
            yield return afterEvery;
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