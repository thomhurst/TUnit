using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Hooks;

namespace TUnit.Engine.Services;

internal class HooksCollector
{
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
    private readonly string _sessionId;

    public HooksCollector(string sessionId)
    {
        _sessionId = sessionId;
        
        CollectDiscoveryHooks();
        CollectHooks();
    }

    private void CollectDiscoveryHooks()
    {
        foreach (var hookSource in Sources.TestDiscoveryHookSources)
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestDiscoveryHooks(_sessionId))
            {
                BeforeTestDiscoveryHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestDiscoveryHooks(_sessionId))
            {
                AfterTestDiscoveryHooks.Add(afterHook);
            }
        }
    }

    private void CollectHooks()
    {
        foreach (var hookSource in Sources.TestHookSources)
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestHooks(_sessionId))
            {
                var beforeList = BeforeTestHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestHooks(_sessionId))
            {
                var afterList = AfterTestHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryTestHooks(_sessionId))
            {
                BeforeEveryTestHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryTestHooks(_sessionId))
            {
                AfterEveryTestHooks.Add(afterHook);
            }
        }

        foreach (var hookSource in Sources.ClassHookSources)
        {
            foreach (var beforeHook in hookSource.CollectBeforeClassHooks(_sessionId))
            {
                var beforeList = BeforeClassHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterClassHooks(_sessionId))
            {
                var afterList = AfterClassHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryClassHooks(_sessionId))
            {
                BeforeEveryClassHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryClassHooks(_sessionId))
            {
                AfterEveryClassHooks.Add(afterHook);
            }
        }

        foreach (var hookSource in Sources.AssemblyHookSources)
        {
            foreach (var beforeHook in hookSource.CollectBeforeAssemblyHooks(_sessionId))
            {
                BeforeAssemblyHooks.GetOrAdd(beforeHook.Assembly, _ => []).Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterAssemblyHooks(_sessionId))
            {
                AfterAssemblyHooks.GetOrAdd(afterHook.Assembly, _ => []).Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryAssemblyHooks(_sessionId))
            {
                BeforeEveryAssemblyHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryAssemblyHooks(_sessionId))
            {
                AfterEveryAssemblyHooks.Add(afterHook);
            }
        }

        foreach (var hookSource in Sources.TestSessionHookSources)
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestSessionHooks(_sessionId))
            {
                BeforeTestSessionHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestSessionHooks(_sessionId))
            {
                AfterTestSessionHooks.Add(afterHook);
            }
        }
    }
}