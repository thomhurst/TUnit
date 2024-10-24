using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Data;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal static class TestDictionary
{
    public static readonly List<ITestSource> TestSources = [];
    
    public static readonly List<ITestHookSource> TestHookSources = [];
    public static readonly List<IClassHookSource> ClassHookSources = [];
    public static readonly List<IAssemblyHookSource> AssemblyHookSources = [];
    public static readonly List<ITestSessionHookSource> TestSessionHookSources = [];
    public static readonly List<ITestDiscoveryHookSource> TestDiscoveryHookSources = [];

    public static readonly ConcurrentDictionary<Type, ClassHookContext> ClassHookContexts = new();
    public static readonly ConcurrentDictionary<Type, List<(string Name, StaticHookMethod HookMethod, LazyHook<string, IHookMessagePublisher> Action)>> ClassSetUps = new();
    public static readonly ConcurrentDictionary<Type, List<(string Name, StaticHookMethod HookMethod, Func<Task> Action)>> ClassCleanUps = new();
    
    public static readonly ConcurrentDictionary<Assembly, AssemblyHookContext> AssemblyHookContexts = new();
    public static readonly GetOnlyDictionary<Assembly, List<(string Name, StaticHookMethod HookMethod, LazyHook<string, IHookMessagePublisher> Action)>> AssemblySetUps = new();
    public static readonly GetOnlyDictionary<Assembly, List<(string Name, StaticHookMethod HookMethod, Func<Task> Action)>> AssemblyCleanUps = new();

    public static readonly ConcurrentDictionary<Type, List<(string Name, int Order, Func<object, DiscoveredTest, Task> Action)>> TestSetUps = new();
    public static readonly ConcurrentDictionary<Type, List<(string Name, int Order, Func<object, DiscoveredTest, Task> Action)>> TestCleanUps = new();
    
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestContext, Task> Action)> GlobalTestSetUps = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestContext, Task> Action)> GlobalTestCleanUps = [];

    public static readonly List<(string Name, StaticHookMethod HookMethod, LazyHook<string, IHookMessagePublisher> Action)> GlobalClassSetUps = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<ClassHookContext, Task> Action)> GlobalClassCleanUps = [];
    
    public static readonly List<(string Name, StaticHookMethod HookMethod, LazyHook<string, IHookMessagePublisher> Action)> GlobalAssemblySetUps = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<AssemblyHookContext, Task> Action)> GlobalAssemblyCleanUps = [];
    
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<BeforeTestDiscoveryContext, Task> Action)> BeforeTestDiscovery = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestDiscoveryContext, Task> Action)> AfterTestDiscovery = [];
    
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestSessionContext, Task> Action)> BeforeTestSession = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestSessionContext, Task> Action)> AfterTestSession = [];
}