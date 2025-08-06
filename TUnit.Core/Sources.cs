using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class Sources
{
    public static readonly ConcurrentQueue<Func<Assembly>> AssemblyLoaders = [];
    public static readonly ConcurrentDictionary<Type, ConcurrentQueue<ITestSource>> TestSources = new(Environment.ProcessorCount * 2, 1000);
    public static readonly ConcurrentQueue<IDynamicTestSource> DynamicTestSources = [];

    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.InstanceHookMethod>> BeforeTestHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.InstanceHookMethod>> AfterTestHooks = new();
    public static readonly ConcurrentBag<Hooks.BeforeTestHookMethod> BeforeEveryTestHooks = new();
    public static readonly ConcurrentBag<Hooks.AfterTestHookMethod> AfterEveryTestHooks = new();
    
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.BeforeClassHookMethod>> BeforeClassHooks = new();
    public static readonly ConcurrentDictionary<Type, ConcurrentBag<Hooks.AfterClassHookMethod>> AfterClassHooks = new();
    public static readonly ConcurrentBag<Hooks.BeforeClassHookMethod> BeforeEveryClassHooks = new();
    public static readonly ConcurrentBag<Hooks.AfterClassHookMethod> AfterEveryClassHooks = new();
    
    public static readonly ConcurrentDictionary<Assembly, ConcurrentBag<Hooks.BeforeAssemblyHookMethod>> BeforeAssemblyHooks = new();
    public static readonly ConcurrentDictionary<Assembly, ConcurrentBag<Hooks.AfterAssemblyHookMethod>> AfterAssemblyHooks = new();
    public static readonly ConcurrentBag<Hooks.BeforeAssemblyHookMethod> BeforeEveryAssemblyHooks = new();
    public static readonly ConcurrentBag<Hooks.AfterAssemblyHookMethod> AfterEveryAssemblyHooks = new();
    
    public static readonly ConcurrentBag<Hooks.BeforeTestSessionHookMethod> BeforeTestSessionHooks = [];
    public static readonly ConcurrentBag<Hooks.AfterTestSessionHookMethod> AfterTestSessionHooks = [];
    public static readonly ConcurrentBag<Hooks.BeforeTestDiscoveryHookMethod> BeforeTestDiscoveryHooks = [];
    public static readonly ConcurrentBag<Hooks.AfterTestDiscoveryHookMethod> AfterTestDiscoveryHooks = [];

    public static readonly ConcurrentQueue<Func<Task>> GlobalInitializers = [];
    public static readonly ConcurrentQueue<IPropertySource> PropertySources = [];
}
