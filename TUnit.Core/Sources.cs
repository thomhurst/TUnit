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
    public static readonly ConcurrentQueue<ITestSource> TestSources = [];
    public static readonly ConcurrentQueue<IDynamicTestSource> DynamicTestSources = [];

    public static readonly ConcurrentQueue<ITestHookSource> TestHookSources = [];
    public static readonly ConcurrentQueue<IClassHookSource> ClassHookSources = [];
    public static readonly ConcurrentQueue<IAssemblyHookSource> AssemblyHookSources = [];
    public static readonly ConcurrentQueue<ITestSessionHookSource> TestSessionHookSources = [];
    public static readonly ConcurrentQueue<ITestDiscoveryHookSource> TestDiscoveryHookSources = [];

    public static readonly ConcurrentQueue<Func<Task>> GlobalInitializers = [];
    public static readonly ConcurrentQueue<IPropertySource> PropertySources = [];
}
