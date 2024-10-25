using System.Diagnostics;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
[StackTraceHidden]
public class SourceRegistrar
{
    public static void Register(ITestSource testSource)
    {
        Sources.TestSources.Add(testSource);
    }
	
    public static void RegisterTestHookSource(ITestHookSource testSource)
    {
        Sources.TestHookSources.Add(testSource);
    }
	
    public static void RegisterClassHookSource(IClassHookSource testSource)
    {
        Sources.ClassHookSources.Add(testSource);
    }
	
    public static void RegisterAssemblyHookSource(IAssemblyHookSource testSource)
    {
        Sources.AssemblyHookSources.Add(testSource);
    }
	
    public static void RegisterTestSessionHookSource(ITestSessionHookSource testSource)
    {
        Sources.TestSessionHookSources.Add(testSource);
    }
	
    public static void RegisterTestDiscoveryHookSource(ITestDiscoveryHookSource testSource)
    {
        Sources.TestDiscoveryHookSources.Add(testSource);
    }
}