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

    /// <summary>
    /// Checks if any hooks have been registered in the Sources
    /// </summary>
    public static bool HasAnyHooks()
    {
        return !BeforeTestHooks.IsEmpty || !AfterTestHooks.IsEmpty ||
               !BeforeEveryTestHooks.IsEmpty || !AfterEveryTestHooks.IsEmpty ||
               !BeforeClassHooks.IsEmpty || !AfterClassHooks.IsEmpty ||
               !BeforeEveryClassHooks.IsEmpty || !AfterEveryClassHooks.IsEmpty ||
               !BeforeAssemblyHooks.IsEmpty || !AfterAssemblyHooks.IsEmpty ||
               !BeforeEveryAssemblyHooks.IsEmpty || !AfterEveryAssemblyHooks.IsEmpty ||
               !BeforeTestSessionHooks.IsEmpty || !AfterTestSessionHooks.IsEmpty ||
               !BeforeTestDiscoveryHooks.IsEmpty || !AfterTestDiscoveryHooks.IsEmpty;
    }

    /// <summary>
    /// Clears all hook collections by draining their contents
    /// </summary>
    public static void ClearAllHooks()
    {
        // Clear ConcurrentDictionary collections
        BeforeTestHooks.Clear();
        AfterTestHooks.Clear();
        BeforeClassHooks.Clear();
        AfterClassHooks.Clear();
        BeforeAssemblyHooks.Clear();
        AfterAssemblyHooks.Clear();

        // Drain ConcurrentBag collections (no Clear method in .NET Standard 2.0)
        while (BeforeEveryTestHooks.TryTake(out _)) { }
        while (AfterEveryTestHooks.TryTake(out _)) { }
        while (BeforeEveryClassHooks.TryTake(out _)) { }
        while (AfterEveryClassHooks.TryTake(out _)) { }
        while (BeforeEveryAssemblyHooks.TryTake(out _)) { }
        while (AfterEveryAssemblyHooks.TryTake(out _)) { }
        while (BeforeTestSessionHooks.TryTake(out _)) { }
        while (AfterTestSessionHooks.TryTake(out _)) { }
        while (BeforeTestDiscoveryHooks.TryTake(out _)) { }
        while (AfterTestDiscoveryHooks.TryTake(out _)) { }
    }
}
