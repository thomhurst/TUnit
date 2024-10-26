using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal static class Sources
{
    public static readonly List<ITestSource> TestSources = [];
    
    public static readonly List<ITestHookSource> TestHookSources = [];
    public static readonly List<IClassHookSource> ClassHookSources = [];
    public static readonly List<IAssemblyHookSource> AssemblyHookSources = [];
    public static readonly List<ITestSessionHookSource> TestSessionHookSources = [];
    public static readonly List<ITestDiscoveryHookSource> TestDiscoveryHookSources = [];
}