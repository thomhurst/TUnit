using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Logging;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class TestHookOrchestrator(HooksCollectorBase hooksCollector)
{
    public async Task<ExecutionContext[]> ExecuteBeforeHooks(DiscoveredTest discoveredTest, CancellationToken cancellationToken)
    {
        var beforeHooks = CollectBeforeHooks(
            discoveredTest.TestContext.TestDetails.ClassInstance,
            discoveredTest);

        foreach (var executableHook in beforeHooks)
        {
            await Timings.Record($"Before(Test): {executableHook.Name}", discoveredTest.TestContext, () =>
            {
                try
                {
                    return executableHook.ExecuteAsync(discoveredTest.TestContext, cancellationToken);
                }
                catch (Exception e)
                {
                    throw new HookFailedException($"Error executing [Before(Test)] hook: {executableHook.MethodInfo.Type.FullName}.{executableHook.Name}", e);
                }
            });
            
            discoveredTest.TestContext.RestoreExecutionContext();
        }

        return discoveredTest.TestContext.GetExecutionContexts();
    }
    
    internal IEnumerable<IExecutableHook<TestContext>> CollectBeforeHooks(object classInstance, DiscoveredTest discoveredTest)
    {
        var testClassType = classInstance.GetType();
        
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType).Reverse();

        foreach (var type in typesIncludingBase)
        {
            foreach (var instanceHookMethod in GetBeforeHooks(type)
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

    private IReadOnlyCollection<InstanceHookMethod> GetBeforeHooks(Type type)
    {
        if (hooksCollector.BeforeTestHooks.TryGetValue(type, out var hooks))
        {
            return hooks;
        }
        
        if (type.IsGenericType && hooksCollector.BeforeTestHooks.TryGetValue(type.GetGenericTypeDefinition(), out hooks))
        {
            return hooks;
        }

        return [];
    }
    
    private IReadOnlyCollection<InstanceHookMethod> GetAfterHooks(Type type)
    {
        if (hooksCollector.AfterTestHooks.TryGetValue(type, out var hooks))
        {
            return hooks;
        }
        
        if (type.IsGenericType && hooksCollector.AfterTestHooks.TryGetValue(type.GetGenericTypeDefinition(), out hooks))
        {
            return hooks;
        }

        return [];
    }

    internal IEnumerable<IExecutableHook<TestContext>> CollectAfterHooks(object classInstance, DiscoveredTest discoveredTest, List<Exception> cleanUpExceptions)
    {
        var testClassType = classInstance.GetType();
        
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            foreach (var instanceHookMethod in GetAfterHooks(type)
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