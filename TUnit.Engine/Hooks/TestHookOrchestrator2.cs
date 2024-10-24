using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class TestHookOrchestrator2(HooksCollector hooksCollector)
{
    private readonly GetOnlyDictionary<Type, Task> _before = new();
    private readonly GetOnlyDictionary<Type, Task> _after = new();

    public async Task ExecuteBeforeHooks(object classInstance, DiscoveredTest discoveredTest)
    {
        var context = GetClassHookContext(testClassType);
        
        await _before.GetOrAdd(testClassType, async _ =>
            {
                // Reverse so base types are first - We'll run those ones first
                var typesIncludingBase = GetTypesIncludingBase(testClassType).Reverse();

                foreach (var type in typesIncludingBase)
                {
                    foreach (var beforeEveryClass in hooksCollector.BeforeEveryClassHooks)
                    {
                        await beforeEveryClass.Body(context, default);
                    }

                    var list = hooksCollector.BeforeClassHooks.GetOrAdd(type, _ => []);

                    foreach (var staticHookMethod in list)
                    {
                        await staticHookMethod.Body(context, default);
                    }
                }
            });
    }

    public async Task ExecuteAfterHooks(object classInstance, DiscoveredTest testContext, List<Exception> cleanUpExceptions)
    {
        await _after.GetOrAdd(testClassType, async _ =>
        {
            var context = GetClassHookContext(testClassType);

            var typesIncludingBase = GetTypesIncludingBase(testClassType);

            foreach (var type in typesIncludingBase)
            {
                foreach (var testEndEventsObject in testContext.GetLastTestInClassEventObjects())
                {
                    await RunHelpers.RunValueTaskSafelyAsync(
                        () => testEndEventsObject.IfLastTestInClass(context, testContext),
                        cleanUpExceptions);
                }

                await TestDataContainer.OnLastInstance(testClassType);

                var list = hooksCollector.AfterClassHooks.GetOrAdd(type, _ => []);

                foreach (var staticHookMethod in list)
                {
                    await RunHelpers.RunSafelyAsync(() => staticHookMethod.Body(context, default), cleanUpExceptions);
                }
                
                foreach (var afterEveryClass in hooksCollector.AfterEveryClassHooks)
                {
                    await RunHelpers.RunSafelyAsync(() => afterEveryClass.Body(context, default), cleanUpExceptions);
                }
            }
        });
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

    private static ClassHookContext GetClassHookContext(Type type)
    {
        lock (type)
        {
            return TestDictionary.ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
            {
                ClassType = type
            });
        }
    }
}