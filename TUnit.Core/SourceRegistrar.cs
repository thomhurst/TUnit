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
	
    public static void Register(ITestHookSource testSource)
    {
        Sources.TestHookSources.Add(testSource);
    }
	
    public static void Register(IClassHookSource testSource)
    {
        Sources.ClassHookSources.Add(testSource);
    }
	
    public static void Register(IAssemblyHookSource testSource)
    {
        Sources.AssemblyHookSources.Add(testSource);
    }
	
    public static void Register(ITestSessionHookSource testSource)
    {
        Sources.TestSessionHookSources.Add(testSource);
    }
	
    public static void Register(ITestDiscoveryHookSource testSource)
    {
        Sources.TestDiscoveryHookSources.Add(testSource);
    }
}