using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal static class Sources
{
    public static readonly ConcurrentQueue<Func<Assembly>> AssemblyLoaders = [];
    public static readonly ConcurrentDictionary<Type, ConcurrentQueue<ITestSource>> TestSources = new(Environment.ProcessorCount * 2, 1000);
    public static readonly ConcurrentQueue<IDynamicTestSource> DynamicTestSources = [];

    // Type-indexed hook storage for O(1) lookup
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.InstanceHookMethod>> BeforeTestHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.InstanceHookMethod>> AfterTestHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.StaticHookMethod<TestContext>>> BeforeEveryTestHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.StaticHookMethod<TestContext>>> AfterEveryTestHooks = new();
    
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.StaticHookMethod<ClassHookContext>>> BeforeClassHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.StaticHookMethod<ClassHookContext>>> AfterClassHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.StaticHookMethod<ClassHookContext>>> BeforeEveryClassHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.StaticHookMethod<ClassHookContext>>> AfterEveryClassHooks = new();
    
    public static readonly ConcurrentDictionary<Assembly, ConcurrentBag<Hooks.StaticHookMethod<AssemblyHookContext>>> BeforeAssemblyHooks = new();
    public static readonly ConcurrentDictionary<Assembly, ConcurrentBag<Hooks.StaticHookMethod<AssemblyHookContext>>> AfterAssemblyHooks = new();
    public static readonly ConcurrentDictionary<Assembly, ConcurrentBag<Hooks.StaticHookMethod<AssemblyHookContext>>> BeforeEveryAssemblyHooks = new();
    public static readonly ConcurrentDictionary<Assembly, ConcurrentBag<Hooks.StaticHookMethod<AssemblyHookContext>>> AfterEveryAssemblyHooks = new();
    
    public static readonly ConcurrentBag<Hooks.StaticHookMethod<TestSessionContext>> BeforeTestSessionHooks = [];
    public static readonly ConcurrentBag<Hooks.StaticHookMethod<TestSessionContext>> AfterTestSessionHooks = [];
    public static readonly ConcurrentBag<Hooks.StaticHookMethod<BeforeTestDiscoveryContext>> BeforeTestDiscoveryHooks = [];
    public static readonly ConcurrentBag<Hooks.StaticHookMethod<TestDiscoveryContext>> AfterTestDiscoveryHooks = [];

    public static readonly ConcurrentQueue<Func<Task>> GlobalInitializers = [];
    public static readonly ConcurrentQueue<IPropertySource> PropertySources = [];
}
