using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Engine.Services;

internal class HooksCollector(string sessionId)
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

    public void CollectDiscoveryHooks()
    {
        while (Sources.TestDiscoveryHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestDiscoveryHooks(sessionId))
            {
                BeforeTestDiscoveryHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestDiscoveryHooks(sessionId))
            {
                AfterTestDiscoveryHooks.Add(afterHook);
            }
        }
    }

    public void CollectionTestSessionHooks()
    {
        while (Sources.TestSessionHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestSessionHooks(sessionId))
            {
                BeforeTestSessionHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestSessionHooks(sessionId))
            {
                AfterTestSessionHooks.Add(afterHook);
            }
        }
    }

    public void CollectHooks()
    {
        while (Sources.TestHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestHooks(sessionId))
            {
                var beforeList = BeforeTestHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestHooks(sessionId))
            {
                var afterList = AfterTestHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryTestHooks(sessionId))
            {
                BeforeEveryTestHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryTestHooks(sessionId))
            {
                AfterEveryTestHooks.Add(afterHook);
            }
        }

        while (Sources.ClassHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeClassHooks(sessionId))
            {
                var beforeList = BeforeClassHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterClassHooks(sessionId))
            {
                var afterList = AfterClassHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryClassHooks(sessionId))
            {
                BeforeEveryClassHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryClassHooks(sessionId))
            {
                AfterEveryClassHooks.Add(afterHook);
            }
        }

        while (Sources.AssemblyHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeAssemblyHooks(sessionId))
            {
                BeforeAssemblyHooks.GetOrAdd(beforeHook.Assembly, _ => []).Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterAssemblyHooks(sessionId))
            {
                AfterAssemblyHooks.GetOrAdd(afterHook.Assembly, _ => []).Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryAssemblyHooks(sessionId))
            {
                BeforeEveryAssemblyHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryAssemblyHooks(sessionId))
            {
                AfterEveryAssemblyHooks.Add(afterHook);
            }
        }
    }
}