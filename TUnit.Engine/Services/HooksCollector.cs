using System.Collections.Concurrent;
using System.Reflection;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.Logging;
using TUnit.Core;
using TUnit.Core.Data;

namespace TUnit.Engine.Services;

internal class HooksCollector(ITUnitMessageBus messageBus, ILoggerFactory loggerFactory)
{
    internal readonly ILogger<TestsConstructor> Logger = loggerFactory.CreateLogger<TestsConstructor>();
    
    internal readonly List<StaticHookMethod<BeforeTestDiscoveryContext>> BeforeTestDiscoveryHooks = []; 
    internal readonly List<StaticHookMethod<TestSessionContext>> BeforeTestSessionHooks = []; 
    internal readonly GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> BeforeAssemblyHooks = new (); 
    internal readonly GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> BeforeClassHooks = new (); 
    internal readonly GetOnlyDictionary<Type, List<InstanceHookMethod>> BeforeTestHooks = new (); 
    
    internal readonly List<StaticHookMethod<AssemblyHookContext>> BeforeEveryAssemblyHooks = []; 
    internal readonly List<StaticHookMethod<ClassHookContext>> BeforeEveryClassHooks = []; 
    internal readonly List<StaticHookMethod<TestContext>> BeforeEveryTestHooks = []; 
    
    internal readonly List<StaticHookMethod<TestDiscoveryContext>> AfterTestDiscoveryHooks = []; 
    internal readonly List<StaticHookMethod<TestSessionContext>> AfterTestSessionHooks = []; 
    internal readonly GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> AfterAssemblyHooks = new (); 
    internal readonly GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> AfterClassHooks = new (); 
    internal readonly GetOnlyDictionary<Type, List<InstanceHookMethod>> AfterTestHooks = new (); 
    
    internal readonly List<StaticHookMethod<AssemblyHookContext>> AfterEveryAssemblyHooks = []; 
    internal readonly List<StaticHookMethod<ClassHookContext>> AfterEveryClassHooks = []; 
    internal readonly List<StaticHookMethod<TestContext>> AfterEveryTestHooks = []; 
    
    public void CollectHooks()
    {
        TestDictionary.TestHookSources.ForEach(hookSource =>
        {
            foreach (var beforeHook in hookSource.CollectBeforeHooks())
            {
                var beforeList = BeforeTestHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterHooks())
            {
                var afterList = BeforeTestHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
        });
        
        TestDictionary.ClassHookSources.ForEach(hookSource =>
        {
            foreach (var beforeHook in hookSource.CollectBeforeHooks())
            {
                var beforeList = BeforeClassHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterHooks())
            {
                var afterList = AfterClassHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
        });
        
        TestDictionary.AssemblyHookSources.ForEach(hookSource =>
        {
            foreach (var beforeHook in hookSource.CollectBeforeHooks())
            {
                var beforeList = BeforeAssemblyHooks.GetOrAdd(beforeHook.Assembly, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterHooks())
            {
                var afterList = AfterAssemblyHooks.GetOrAdd(afterHook.Assembly, _ => []);
                afterList.Add(afterHook);
            }
        });
        
        TestDictionary.TestSessionHookSources.ForEach(hookSource =>
        {
            foreach (var beforeHook in hookSource.CollectBeforeHooks())
            {
                BeforeTestSessionHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterHooks())
            {
                AfterTestSessionHooks.Add(afterHook);
            }
        });
        
        TestDictionary.TestDiscoveryHookSources.ForEach(hookSource =>
        {
            foreach (var beforeHook in hookSource.CollectBeforeHooks())
            {
                BeforeTestDiscoveryHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterHooks())
            {
                AfterTestDiscoveryHooks.Add(afterHook);
            }
        });
    }
}