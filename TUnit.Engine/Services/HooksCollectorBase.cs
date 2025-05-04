using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Hooks;

namespace TUnit.Engine.Services;

internal abstract class HooksCollectorBase(string sessionId)
{
    public string SessionId { get; } = sessionId;
    
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
    public abstract void CollectDiscoveryHooks();
    public abstract void CollectionTestSessionHooks();
    public abstract void CollectHooks();
}