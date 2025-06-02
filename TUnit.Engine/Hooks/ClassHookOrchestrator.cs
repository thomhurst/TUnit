using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Extensions;
using TUnit.Core.Hooks;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

internal class ClassHookOrchestrator(InstanceTracker instanceTracker, HooksCollectorBase hooksCollector)
{
    private readonly ConcurrentDictionary<Type, bool> _beforeHooksReached = new();

    internal GetOnlyDictionary<Type, TaskCompletionSource<bool>> PreviouslyRunBeforeHooks { get; } = new();

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
    
    public async Task ExecuteBeforeClassHooks(TestContext testContext)
    {
        var classHookContext = testContext.ClassContext;

        var classHooksTaskCompletionSource = PreviouslyRunBeforeHooks.GetOrAdd(
            testContext.TestDetails.TestClass.Type, _ => new TaskCompletionSource<bool>(),
            out var classHooksTaskPreviouslyExisted);

        if (classHooksTaskPreviouslyExisted)
        {
            await classHooksTaskCompletionSource.Task;
            return;
        }

        try
        {
            var beforeClassHooks = CollectBeforeHooks(testContext.TestDetails.TestClass.Type);

            ClassHookContext.Current = classHookContext;

            foreach (var beforeHook in beforeClassHooks)
            {
                classHookContext.RestoreExecutionContext();

                try
                {
                    await beforeHook.ExecuteAsync(classHookContext, CancellationToken.None);
                }
                catch (Exception e)
                {
                    throw new HookFailedException($"Error executing [Before(Class)] hook: {beforeHook.MethodInfo.Type.FullName}.{beforeHook.Name}", e);
                }
            }

            ClassHookContext.Current = null;
            classHooksTaskCompletionSource.SetResult(false);
        }
        catch (Exception e)
        {
            classHooksTaskCompletionSource.SetException(e);
            throw;
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
        
        foreach (var testEndEventsObject in testContext.GetLastTestInClassEventObjects())
        {
            yield return new LastTestInClassAdapter(testEndEventsObject, testContext);
        }
        
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
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
}