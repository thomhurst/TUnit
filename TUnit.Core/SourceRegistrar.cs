using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
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
        Sources.TestSources.Enqueue(testSource);
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
    /// Registers a property initializer for a specific type that takes a DataGeneratorMetadata parameter.
    /// </summary>
    /// <typeparam name="T">The type to register the initializer for.</typeparam>
    public static void RegisterProperty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] T>()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.CanWrite && p.HasAttribute<IAsyncDataSourceGeneratorAttribute>())
            .ToArray();

        if (properties.Length == 0)
        {
            return;
        }

        Sources.Properties.TryAdd(typeof(T), properties);
    }

    private static bool IsEvent(Type type)
    {
        return type.IsAssignableTo<IAsyncInitializer>()
            || type.IsAssignableTo<IAsyncDisposable>()
            || type.IsAssignableTo<IEventReceiver>();
    }

    public static void RegisterGlobalInitializer(Func<Task> initializer)
    {
        Sources.GlobalInitializers.Add(initializer);
    }
}
