using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Extensions;
using TUnit.Core.Hooks;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class ClassHookOrchestrator(InstanceTracker instanceTracker, HooksCollector hooksCollector)
{
    private readonly ConcurrentDictionary<Type, ClassHookContext> _classHookContexts = new();
    
    private readonly ConcurrentDictionary<Type, bool> _beforeHooksReached = new();

    public IEnumerable<StaticHookMethod<ClassHookContext>> CollectBeforeHooks(Type testClassType)
    {
        _beforeHooksReached.GetOrAdd(testClassType, true);

        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType).Reverse();

        foreach (var type in typesIncludingBase)
        {
            foreach (var beforeEveryClass in hooksCollector.BeforeEveryClassHooks
                         .OrderBy(x => x.Order))
            {
                yield return beforeEveryClass;
            }

            var list = hooksCollector.BeforeClassHooks
                .GetOrAdd(type, _ => []).OrderBy(x => x.Order);

            foreach (var staticHookMethod in list)
            {
                yield return staticHookMethod;
            }
        }
    }

    public IEnumerable<IExecutableHook<ClassHookContext>> CollectAfterHooks(
        TestContext testContext, 
        Type testClassType)
    {
        if (!instanceTracker.IsLastTestForType(testClassType))
        {
            // Only run one time clean downs when no instances are left!
           yield break;
        }
        
        if (!_beforeHooksReached.TryGetValue(testClassType, out _))
        {
            // The before hooks were never hit, meaning no tests were executed, so nothing to clean up.
            yield break;
        }
        
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            foreach (var testEndEventsObject in testContext.GetLastTestInClassEventObjects())
            {
                yield return new LastTestInClassAdapter(testEndEventsObject, testContext);
            }
                
            var list = hooksCollector.AfterClassHooks
                .GetOrAdd(type, _ => []).OrderBy(x => x.Order);

            foreach (var staticHookMethod in list)
            {
                yield return staticHookMethod;
            }
                
            foreach (var afterEveryClass in hooksCollector.AfterEveryClassHooks.OrderBy(x => x.Order))
            {
                yield return afterEveryClass;
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

    public ClassHookContext GetContext(Type type)
    {
        return _classHookContexts.GetOrAdd(type, _ => new ClassHookContext
        {
            ClassType = type
        });
    }
}