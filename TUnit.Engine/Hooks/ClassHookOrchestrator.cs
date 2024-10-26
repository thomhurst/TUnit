using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class ClassHookOrchestrator(InstanceTracker instanceTracker, HooksCollector hooksCollector)
{
    private readonly ConcurrentDictionary<Type, ClassHookContext> _classHookContexts = new();

    private readonly GetOnlyDictionary<Type, Task> _before = new();
    private readonly GetOnlyDictionary<Type, Task> _after = new();

    public async Task ExecuteBeforeHooks(Type testClassType)
    {
        await _before.GetOrAdd(testClassType, async _ =>
            {
                var context = GetContext(testClassType);
                
                ClassHookContext.Current = context;

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
                
                ClassHookContext.Current = null;
            });
    }

    public async Task ExecuteCleanUpsIfLastInstance(
        TestContext testContext, 
        Type testClassType,
        List<Exception> cleanUpExceptions
        )
    {
        if (!instanceTracker.IsLastTestForType(testClassType))
        {
            // Only run one time clean downs when no instances are left!
           return;
        }
        
        await _after.GetOrAdd(testClassType, async _ =>
        {
            var context = GetContext(testClassType);
            
            ClassHookContext.Current = context;

            var typesIncludingBase = GetTypesIncludingBase(testClassType);

            foreach (var type in typesIncludingBase)
            {
                foreach (var testEndEventsObject in testContext.GetLastTestInClassEventObjects())
                {
                    await RunHelpers.RunValueTaskSafelyAsync(
                        () => testEndEventsObject.IfLastTestInClass(context, testContext),
                        cleanUpExceptions);
                }
                
                var list = hooksCollector.AfterClassHooks.GetOrAdd(type, _ => []);

                foreach (var staticHookMethod in list)
                {
                    await RunHelpers.RunSafelyAsync(() => staticHookMethod.Body(context, default), cleanUpExceptions);
                }
                
                foreach (var afterEveryClass in hooksCollector.AfterEveryClassHooks)
                {
                    await RunHelpers.RunSafelyAsync(() => afterEveryClass.Body(context, default), cleanUpExceptions);
                }
                
                ClassHookContext.Current = null;
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

    public ClassHookContext GetContext(Type type)
    {
        return _classHookContexts.GetOrAdd(type, _ => new ClassHookContext
        {
            ClassType = type
        });
    }
}