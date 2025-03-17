using System.Diagnostics;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
[StackTraceHidden]
/// <summary>
/// Provides methods to register various sources.
/// </summary>
public class SourceRegistrar
{
    /// <summary>
    /// Registers a test source.
    /// </summary>
    /// <param name="testSource">The test source to register.</param>
    public static void Register(ITestSource testSource)
    {
        Sources.TestSources.Add(testSource);
    }

    /// <summary>
    /// Registers a test hook source.
    /// </summary>
    /// <param name="testSource">The test hook source to register.</param>
    public static void RegisterTestHookSource(ITestHookSource testSource)
    {
        Sources.TestHookSources.Add(testSource);
    }

    /// <summary>
    /// Registers a class hook source.
    /// </summary>
    /// <param name="testSource">The class hook source to register.</param>
    public static void RegisterClassHookSource(IClassHookSource testSource)
    {
        Sources.ClassHookSources.Add(testSource);
    }

    /// <summary>
    /// Registers an assembly hook source.
    /// </summary>
    /// <param name="testSource">The assembly hook source to register.</param>
    public static void RegisterAssemblyHookSource(IAssemblyHookSource testSource)
    {
        Sources.AssemblyHookSources.Add(testSource);
    }

    /// <summary>
    /// Registers a test session hook source.
    /// </summary>
    /// <param name="testSource">The test session hook source to register.</param>
    public static void RegisterTestSessionHookSource(ITestSessionHookSource testSource)
    {
        Sources.TestSessionHookSources.Add(testSource);
    }

    /// <summary>
    /// Registers a test discovery hook source.
    /// </summary>
    /// <param name="testSource">The test discovery hook source to register.</param>
    public static void RegisterTestDiscoveryHookSource(ITestDiscoveryHookSource testSource)
    {
        Sources.TestDiscoveryHookSources.Add(testSource);
    }
}