using System.Collections.Concurrent;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class ClassHookOrchestrator
{
    private static readonly ConcurrentDictionary<Type, List<(string Name, StaticHookMethod HookMethod, Lazy<Task> Action)>> SetUps = new();
    private static readonly ConcurrentDictionary<Type, List<(string Name, StaticHookMethod HookMethod, Func<Task> Action)>> CleanUps = new();
    
    private static readonly ConcurrentDictionary<Type, ClassHookContext> ClassHookContexts = new();
    
    private readonly HookMessagePublisher _hookMessagePublisher;
    private readonly GlobalStaticTestHookOrchestrator _globalStaticTestHookOrchestrator;

    public ClassHookOrchestrator(HookMessagePublisher hookMessagePublisher, GlobalStaticTestHookOrchestrator globalStaticTestHookOrchestrator)
    {
        _hookMessagePublisher = hookMessagePublisher;
        _globalStaticTestHookOrchestrator = globalStaticTestHookOrchestrator;
    }
    
    public static void RegisterBeforeHook(Type type, StaticHookMethod<ClassHookContext> staticMethod)
    {
        var taskFunctions = SetUps.GetOrAdd(type, _ => []);

        taskFunctions.Add((staticMethod.Name, staticMethod, Convert(type, staticMethod)));
    }
    
    public static void RegisterAfterHook(Type type, StaticHookMethod<ClassHookContext> staticMethod)
    {
        var taskFunctions = CleanUps.GetOrAdd(type, _ => []);

        taskFunctions.Add((staticMethod.Name, staticMethod, () =>
        {
            var context = GetClassHookContext(type);
            
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterClassHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        }));
    }
    
    public static void RegisterTestContext(Type type, TestContext testContext)
    {
        var classHookContext = ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
            {
                ClassType = type
            });

        classHookContext.Tests.Add(testContext);
        
        AssemblyHookOrchestrator.RegisterTestContext(type.Assembly, classHookContext);
    }

    private static ClassHookContext GetClassHookContext(Type type)
    {
        lock (type)
        {
            return ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
            {
                ClassType = type
            });
        }
    }
    
    public async Task ExecuteBeforeHooks(ExecuteRequestContext executeRequestContext, Type testClassType)
    {
        var context = GetClassHookContext(testClassType);
        
        // Run Global Hooks First
        await _globalStaticTestHookOrchestrator.ExecuteBeforeHooks(executeRequestContext, context);
            
        // Reverse so base types are first - We'll run those ones first
        var typesIncludingBase = GetTypesIncludingBase(testClassType)
            .Reverse();

        foreach (var type in typesIncludingBase)
        {
            if (!SetUps.TryGetValue(type, out var setUpsForType))
            {
                continue;
            }

            foreach (var setUp in setUpsForType.OrderBy(x => x.HookMethod.Order))
            {
                // As these are lazy we should always get the same Task
                // So we await the same Task to ensure it's finished first
                // and also gives the benefit of rethrowing the same exception if it failed
                await _hookMessagePublisher.Push(executeRequestContext, $"Before Class: {setUp.Name}", setUp.HookMethod, () => setUp.Action.Value);
            }
        }
    }
    
    public async Task ExecuteCleanUpsIfLastInstance(ExecuteRequestContext executeRequestContext, Type testClassType,
        List<Exception> cleanUpExceptions)
    {
        var typesIncludingBase = GetTypesIncludingBase(testClassType);

        foreach (var type in typesIncludingBase)
        {
            if (!InstanceTracker.IsLastTestForType(type))
            {
                // Only run one time clean downs when no instances are left!
                continue;
            }

            await TestDataContainer.OnLastInstance(testClassType);
            
            if (!CleanUps.TryGetValue(type, out var cleanUpsForType))
            {
                continue;
            }

            foreach (var cleanUp in cleanUpsForType.OrderBy(x => x.HookMethod.Order))
            {
                await _hookMessagePublisher.Push(executeRequestContext, $"After Class: {cleanUp.Name}", cleanUp.HookMethod, () => RunHelpers.RunSafelyAsync(cleanUp.Action, cleanUpExceptions));
            }
            
            var context = GetClassHookContext(testClassType);
        
            // Run Global Hooks Last
            await _globalStaticTestHookOrchestrator.ExecuteAfterHooks(executeRequestContext, context, cleanUpExceptions);
        }
    }

    public static IEnumerable<TestContext> GetTestsForType(Type type)
    {
        var context = ClassHookContexts.GetOrAdd(type, new ClassHookContext
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
    
    private static Lazy<Task> Convert(Type type, StaticHookMethod<ClassHookContext> staticMethod)
    {
        return new Lazy<Task>(() =>
        {
            var context = GetClassHookContext(type);
            
            var timeout = staticMethod.Timeout;

            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeClassHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
        });
    }
}