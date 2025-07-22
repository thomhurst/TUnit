using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    public static bool IsEnabled { get; set; }

    /// <summary>
    /// Registers an assembly loader.
    /// </summary>
    /// <param name="assemblyLoader">The assembly loader to register.</param>
    public static void RegisterAssembly(Func<Assembly> assemblyLoader)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return;
        }
#endif

        Sources.AssemblyLoaders.Enqueue(assemblyLoader);
    }

    /// <summary>
    /// Registers a test source.
    /// </summary>
    /// <param name="testSource">The test source to register.</param>
    public static void Register(ITestSource testSource)
    {
        // For backward compatibility, add to all types queue if no type specified
        var allTypesQueue = Sources.TestSources.GetOrAdd(typeof(object), _ => new ConcurrentQueue<ITestSource>());
        allTypesQueue.Enqueue(testSource);
    }
    
    /// <summary>
    /// Registers a test source for a specific test class type.
    /// </summary>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="testSource">The test source to register.</param>
    public static void Register(Type testClassType, ITestSource testSource)
    {
        var queue = Sources.TestSources.GetOrAdd(testClassType, _ => new ConcurrentQueue<ITestSource>());
        queue.Enqueue(testSource);
    }

    /// <summary>
    /// Registers a test source.
    /// </summary>
    /// <param name="testSource">The test source to register.</param>
    public static void RegisterDynamic(IDynamicTestSource testSource)
    {
        Sources.DynamicTestSources.Enqueue(testSource);
    }

    /// <summary>
    /// Registers a test hook source.
    /// </summary>
    /// <param name="testSource">The test hook source to register.</param>
    public static void RegisterTestHookSource(ITestHookSource testSource)
    {
        Sources.TestHookSources.Enqueue(testSource);
    }

    /// <summary>
    /// Registers a class hook source.
    /// </summary>
    /// <param name="testSource">The class hook source to register.</param>
    public static void RegisterClassHookSource(IClassHookSource testSource)
    {
        Sources.ClassHookSources.Enqueue(testSource);
    }

    /// <summary>
    /// Registers an assembly hook source.
    /// </summary>
    /// <param name="testSource">The assembly hook source to register.</param>
    public static void RegisterAssemblyHookSource(IAssemblyHookSource testSource)
    {
        Sources.AssemblyHookSources.Enqueue(testSource);
    }

    /// <summary>
    /// Registers a test session hook source.
    /// </summary>
    /// <param name="testSource">The test session hook source to register.</param>
    public static void RegisterTestSessionHookSource(ITestSessionHookSource testSource)
    {
        Sources.TestSessionHookSources.Enqueue(testSource);
    }

    /// <summary>
    /// Registers a test discovery hook source.
    /// </summary>
    /// <param name="testSource">The test discovery hook source to register.</param>
    public static void RegisterTestDiscoveryHookSource(ITestDiscoveryHookSource testSource)
    {
        Sources.TestDiscoveryHookSources.Enqueue(testSource);
    }

    /// <summary>
    /// Registers a global initializer.
    /// </summary>
    /// <param name="initializer">The initializer to register.</param>
    public static void RegisterGlobalInitializer(Func<Task> initializer)
    {
        Sources.GlobalInitializers.Enqueue(initializer);
    }

    /// <summary>
    /// Registers a property source (for property injection).
    /// </summary>
    /// <param name="propertySource">The property source to register.</param>
    public static void RegisterProperty(IPropertySource propertySource)
    {
        Sources.PropertySources.Enqueue(propertySource);
    }
}
