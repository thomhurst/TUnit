using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Hooks;

namespace TUnit.Engine.Services;

internal class SourceGeneratedHooksCollector(string sessionId) : HooksCollectorBase(sessionId)
{
    private protected override List<StaticHookMethod<BeforeTestDiscoveryContext>> CollectBeforeTestDiscoveryHooks()
    {
        var result = new List<StaticHookMethod<BeforeTestDiscoveryContext>>();
        foreach (var hookSource in Sources.TestDiscoveryHookSources)
        {
            result.AddRange(hookSource.CollectBeforeTestDiscoveryHooks(SessionId));
        }
        return result;
    }

    private protected override List<StaticHookMethod<TestSessionContext>> CollectBeforeTestSessionHooks()
    {
        var result = new List<StaticHookMethod<TestSessionContext>>();
        
        foreach (var hookSource in Sources.TestSessionHookSources)
        {
            result.AddRange(hookSource.CollectBeforeTestSessionHooks(SessionId));
        }
        
        return result;
    }

    private protected override GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> CollectBeforeAssemblyHooks()
    {
        var dict = new GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>>();
        foreach (var hookSource in Sources.AssemblyHookSources)
        {
            foreach (var beforeHook in hookSource.CollectBeforeAssemblyHooks(SessionId))
            {
                var list = dict.GetOrAdd(beforeHook.Assembly, _ => new List<StaticHookMethod<AssemblyHookContext>>());
                list.Add(beforeHook);
            }
        }
        return dict;
    }

    private protected override GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> CollectBeforeClassHooks()
    {
        var dict = new GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>>();
        foreach (var hookSource in Sources.ClassHookSources)
        {
            foreach (var beforeHook in hookSource.CollectBeforeClassHooks(SessionId))
            {
                var list = dict.GetOrAdd(beforeHook.ClassType, _ => new List<StaticHookMethod<ClassHookContext>>());
                list.Add(beforeHook);
            }
        }
        return dict;
    }

    private protected override GetOnlyDictionary<Type, List<InstanceHookMethod>> CollectBeforeTestHooks()
    {
        var dict = new GetOnlyDictionary<Type, List<InstanceHookMethod>>();
        foreach (var hookSource in Sources.TestHookSources)
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestHooks(SessionId))
            {
                var list = dict.GetOrAdd(beforeHook.ClassType, _ => new List<InstanceHookMethod>());
                list.Add(beforeHook);
            }
        }
        return dict;
    }

    private protected override List<StaticHookMethod<AssemblyHookContext>> CollectBeforeEveryAssemblyHooks()
    {
        var result = new List<StaticHookMethod<AssemblyHookContext>>();
        foreach (var hookSource in Sources.AssemblyHookSources)
        {
            result.AddRange(hookSource.CollectBeforeEveryAssemblyHooks(SessionId));
        }
        return result;
    }

    private protected override List<StaticHookMethod<ClassHookContext>> CollectBeforeEveryClassHooks()
    {
        var result = new List<StaticHookMethod<ClassHookContext>>();
        foreach (var hookSource in Sources.ClassHookSources)
        {
            result.AddRange(hookSource.CollectBeforeEveryClassHooks(SessionId));
        }
        return result;
    }

    private protected override List<StaticHookMethod<TestContext>> CollectBeforeEveryTestHooks()
    {
        var result = new List<StaticHookMethod<TestContext>>();
        foreach (var hookSource in Sources.TestHookSources)
        {
            result.AddRange(hookSource.CollectBeforeEveryTestHooks(SessionId));
        }
        return result;
    }

    private protected override List<StaticHookMethod<TestDiscoveryContext>> CollectAfterTestDiscoveryHooks()
    {
        var result = new List<StaticHookMethod<TestDiscoveryContext>>();
        foreach (var hookSource in Sources.TestDiscoveryHookSources)
        {
            result.AddRange(hookSource.CollectAfterTestDiscoveryHooks(SessionId));
        }
        return result;
    }

    private protected override List<StaticHookMethod<TestSessionContext>> CollectAfterTestSessionHooks()
    {
        var result = new List<StaticHookMethod<TestSessionContext>>();
        foreach (var hookSource in Sources.TestSessionHookSources)
        {
            result.AddRange(hookSource.CollectAfterTestSessionHooks(SessionId));
        }
        return result;
    }

    private protected override GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> CollectAfterAssemblyHooks()
    {
        var dict = new GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>>();
        foreach (var hookSource in Sources.AssemblyHookSources)
        {
            foreach (var afterHook in hookSource.CollectAfterAssemblyHooks(SessionId))
            {
                var list = dict.GetOrAdd(afterHook.Assembly, _ => new List<StaticHookMethod<AssemblyHookContext>>());
                list.Add(afterHook);
            }
        }
        return dict;
    }

    private protected override GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> CollectAfterClassHooks()
    {
        var dict = new GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>>();
        foreach (var hookSource in Sources.ClassHookSources)
        {
            foreach (var afterHook in hookSource.CollectAfterClassHooks(SessionId))
            {
                var list = dict.GetOrAdd(afterHook.ClassType, _ => new List<StaticHookMethod<ClassHookContext>>());
                list.Add(afterHook);
            }
        }
        return dict;
    }

    private protected override GetOnlyDictionary<Type, List<InstanceHookMethod>> CollectAfterTestHooks()
    {
        var dict = new GetOnlyDictionary<Type, List<InstanceHookMethod>>();
        foreach (var hookSource in Sources.TestHookSources)
        {
            foreach (var afterHook in hookSource.CollectAfterTestHooks(SessionId))
            {
                var list = dict.GetOrAdd(afterHook.ClassType, _ => new List<InstanceHookMethod>());
                list.Add(afterHook);
            }
        }
        return dict;
    }

    private protected override List<StaticHookMethod<AssemblyHookContext>> CollectAfterEveryAssemblyHooks()
    {
        var result = new List<StaticHookMethod<AssemblyHookContext>>();
        foreach (var hookSource in Sources.AssemblyHookSources)
        {
            result.AddRange(hookSource.CollectAfterEveryAssemblyHooks(SessionId));
        }
        return result;
    }

    private protected override List<StaticHookMethod<ClassHookContext>> CollectAfterEveryClassHooks()
    {
        var result = new List<StaticHookMethod<ClassHookContext>>();
        foreach (var hookSource in Sources.ClassHookSources)
        {
            result.AddRange(hookSource.CollectAfterEveryClassHooks(SessionId));
        }
        return result;
    }

    private protected override List<StaticHookMethod<TestContext>> CollectAfterEveryTestHooks()
    {
        var result = new List<StaticHookMethod<TestContext>>();
        foreach (var hookSource in Sources.TestHookSources)
        {
            result.AddRange(hookSource.CollectAfterEveryTestHooks(SessionId));
        }
        return result;
    }
}

