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
        var allTypesQueue = Sources.TestSources.GetOrAdd(typeof(object), static _ => new ConcurrentQueue<ITestSource>());
        allTypesQueue.Enqueue(testSource);
    }

    /// <summary>
    /// Registers a test source for a specific test class type.
    /// </summary>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="testSource">The test source to register.</param>
    public static void Register(Type testClassType, ITestSource testSource)
    {
        var queue = Sources.TestSources.GetOrAdd(testClassType, static _ => new ConcurrentQueue<ITestSource>());
        queue.Enqueue(testSource);
    }

    /// <summary>
    /// Registers a test source for a specific test class type using delegates.
    /// This avoids allocating a unique TestSource type per class, reducing JIT overhead.
    /// </summary>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="getTests">Delegate that returns test metadata.</param>
    /// <param name="enumerateDescriptors">Delegate that enumerates test descriptors.</param>
    public static void Register(
        Type testClassType,
        Func<string, IReadOnlyList<TestMetadata>> getTests,
        Func<IEnumerable<TestDescriptor>> enumerateDescriptors)
    {
        Register(testClassType, new DelegateTestSource(getTests, enumerateDescriptors));
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

    /// <summary>
    /// Registers a hook into a type-keyed dictionary and returns a dummy value for use as a field initializer.
    /// </summary>
    public static int RegisterHook<T>(ConcurrentDictionary<Type, ConcurrentBag<T>> dictionary, Type key, T hook)
    {
        dictionary.GetOrAdd(key, static _ => new ConcurrentBag<T>()).Add(hook);
        return 0;
    }

    /// <summary>
    /// Registers a hook into an assembly-keyed dictionary and returns a dummy value for use as a field initializer.
    /// </summary>
    public static int RegisterHook<T>(ConcurrentDictionary<Assembly, ConcurrentBag<T>> dictionary, Assembly key, T hook)
    {
        dictionary.GetOrAdd(key, static _ => new ConcurrentBag<T>()).Add(hook);
        return 0;
    }

    /// <summary>
    /// Registers a hook into a global bag and returns a dummy value for use as a field initializer.
    /// </summary>
    public static int RegisterHook<T>(ConcurrentBag<T> bag, T hook)
    {
        bag.Add(hook);
        return 0;
    }

    /// <summary>
    /// Wrapper around <see cref="Register(Type, ITestSource)"/> that returns a dummy value for use as a field initializer.
    /// </summary>
    public static int RegisterReturn(Type testClassType, ITestSource testSource)
    {
        Register(testClassType, testSource);
        return 0;
    }

    /// <summary>
    /// Wrapper around <see cref="Register(Type, Func{string, IReadOnlyList{TestMetadata}}, Func{IEnumerable{TestDescriptor}})"/> that returns a dummy value for use as a field initializer.
    /// </summary>
    public static int RegisterReturn(
        Type testClassType,
        Func<string, IReadOnlyList<TestMetadata>> getTests,
        Func<IEnumerable<TestDescriptor>> enumerateDescriptors)
    {
        Register(testClassType, getTests, enumerateDescriptors);
        return 0;
    }

    /// <summary>
    /// Registers test entries for a class using the TestEntry pattern.
    /// Returns a dummy value for use as a static field initializer.
    /// Multiple calls for the same T are additive — entries accumulate.
    /// </summary>
    public static int RegisterEntries<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] T>(TestEntry<T>[] entries) where T : class
    {
        if (entries is null || entries.Length == 0)
        {
            throw new InvalidOperationException(
                $"Source-generated test registration failed: no entries for '{typeof(T).FullName}'. " +
                "This indicates a source generator bug. Please report this issue.");
        }

        var key = typeof(T);

        while (true)
        {
            if (Sources.TestEntries.TryGetValue(key, out var existing))
            {
                if (existing is TestEntrySource<T> existingSource)
                {
                    existingSource.AddEntries(entries);
                    return 0;
                }

                throw new InvalidOperationException(
                    $"Type mismatch in TestEntries for '{typeof(T).FullName}': expected TestEntrySource<{typeof(T).Name}>, found {existing.GetType().Name}");
            }

            if (Sources.TestEntries.TryAdd(key, new TestEntrySource<T>(entries)))
            {
                return 0;
            }

            // Another thread added between TryGetValue and TryAdd — retry to merge
        }
    }
}
