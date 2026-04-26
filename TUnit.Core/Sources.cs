using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class Sources
{
    public static readonly ConcurrentQueue<Func<Assembly>> AssemblyLoaders = [];
    public static readonly ConcurrentQueue<IDynamicTestSource> DynamicTestSources = [];

    // Hook collections store LazyHookEntry wrappers — the heavy MethodMetadata/ClassMetadata
    // construction is deferred until first Materialize() call (typically during engine
    // discovery/execution rather than at module initialization).
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<LazyHookEntry<InstanceHookMethod>>> BeforeTestHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<LazyHookEntry<InstanceHookMethod>>> AfterTestHooks = new();
    public static readonly ConcurrentBag<LazyHookEntry<BeforeTestHookMethod>> BeforeEveryTestHooks = [];
    public static readonly ConcurrentBag<LazyHookEntry<AfterTestHookMethod>> AfterEveryTestHooks = [];

    public static readonly ConcurrentDictionary<Type, ConcurrentBag<LazyHookEntry<BeforeClassHookMethod>>> BeforeClassHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<LazyHookEntry<AfterClassHookMethod>>> AfterClassHooks = new();
    public static readonly ConcurrentBag<LazyHookEntry<BeforeClassHookMethod>> BeforeEveryClassHooks = [];
    public static readonly ConcurrentBag<LazyHookEntry<AfterClassHookMethod>> AfterEveryClassHooks = [];

    public static readonly ConcurrentDictionary<Assembly, ConcurrentBag<LazyHookEntry<BeforeAssemblyHookMethod>>> BeforeAssemblyHooks = new();
    public static readonly ConcurrentDictionary<Assembly, ConcurrentBag<LazyHookEntry<AfterAssemblyHookMethod>>> AfterAssemblyHooks = new();
    public static readonly ConcurrentBag<LazyHookEntry<BeforeAssemblyHookMethod>> BeforeEveryAssemblyHooks = [];
    public static readonly ConcurrentBag<LazyHookEntry<AfterAssemblyHookMethod>> AfterEveryAssemblyHooks = [];

    public static readonly ConcurrentBag<LazyHookEntry<BeforeTestSessionHookMethod>> BeforeTestSessionHooks = [];
    public static readonly ConcurrentBag<LazyHookEntry<AfterTestSessionHookMethod>> AfterTestSessionHooks = [];
    public static readonly ConcurrentBag<LazyHookEntry<BeforeTestDiscoveryHookMethod>> BeforeTestDiscoveryHooks = [];
    public static readonly ConcurrentBag<LazyHookEntry<AfterTestDiscoveryHookMethod>> AfterTestDiscoveryHooks = [];

    public static readonly ConcurrentQueue<Func<Task>> GlobalInitializers = [];
    public static readonly ConcurrentQueue<IPropertySource> PropertySources = [];

    // TestEntry registration path (source-gen startup performance optimization)
    public static readonly ConcurrentDictionary<Type, ITestEntrySource> TestEntries = new();
}
