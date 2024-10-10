using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class ClassHookOrchestrator(
    HookMessagePublisher hookMessagePublisher,
    GlobalStaticTestHookOrchestrator globalStaticTestHookOrchestrator)
{
    public async Task DiscoverHooks(ExecuteRequestContext context)
    {
        foreach (var (_, list) in TestDictionary.ClassSetUps)
        {
            foreach (var (name, hookMethod, _) in list)
            {
                await hookMessagePublisher.Discover(context.Request.Session.SessionUid.Value, $"Before Class: {name}", hookMethod);
            }
        }

        foreach (var (_, list) in TestDictionary.ClassCleanUps)
        {
            foreach (var (name, hookMethod, _) in list)
            {
                await hookMessagePublisher.Discover(context.Request.Session.SessionUid.Value, $"After Class: {name}", hookMethod);
            }
        }
    }

    internal static ClassHookContext GetClassHookContext(Type type)
    {
        lock (type)
        {
            return TestDictionary.ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
            {
                ClassType = type
            });
        }
    }
    
    public async Task ExecuteBeforeHooks(ExecuteRequestContext executeRequestContext, Type testClassType)
    {
        var context = GetClassHookContext(testClassType);
        
        // Run Global Hooks First
        await globalStaticTestHookOrchestrator.ExecuteBeforeHooks(executeRequestContext, context);
            
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType)
            .Reverse();

        foreach (var type in typesIncludingBase)
        {
            if (!TestDictionary.ClassSetUps.TryGetValue(type, out var setUpsForType))
            {
                continue;
            }

            foreach (var setUp in setUpsForType.OrderBy(x => x.HookMethod.Order))
            {
                // As these are lazy we should always get the same Task
                // So we await the same Task to ensure it's finished first
                // and also gives the benefit of rethrowing the same exception if it failed
                await setUp.Action.Value(executeRequestContext.Request.Session.SessionUid.Value, hookMessagePublisher);
            }
        }
    }
    
    public async Task ExecuteCleanUpsIfLastInstance(ExecuteRequestContext executeRequestContext,
        TestContext testContext, Type testClassType,
        List<Exception> cleanUpExceptions)
    {
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        var context = GetClassHookContext(testClassType);
        
        foreach (var type in typesIncludingBase)
        {
            if (!InstanceTracker.IsLastTestForType(type))
            {
                // Only run one time clean downs when no instances are left!
                continue;
            }
            
            await RunHelpers.RunSafelyAsync(async () =>
            {
                foreach (var testEndEventsObject in testContext.GetTestEndEventsObjects())
                {
                    await testEndEventsObject.IfLastTestInClass(context, testContext);
                }
            }, cleanUpExceptions);
            
            await TestDataContainer.OnLastInstance(testClassType);
            
            if (!TestDictionary.ClassCleanUps.TryGetValue(type, out var cleanUpsForType))
            {
                continue;
            }

            foreach (var cleanUp in cleanUpsForType.OrderBy(x => x.HookMethod.Order))
            {
                await hookMessagePublisher.Push(executeRequestContext.Request.Session.SessionUid.Value, $"After Class: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(cleanUp.Action, cleanUpExceptions));
            }
        }
        
        // Run Global Hooks Last
        await globalStaticTestHookOrchestrator.ExecuteAfterHooks(executeRequestContext, context, cleanUpExceptions);
    }

    public static IEnumerable<TestContext> GetTestsForType(Type type)
    {
        var context = TestDictionary.ClassHookContexts.GetOrAdd(type, new ClassHookContext
        {
            ClassType = type
        });

        return context.Tests;
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