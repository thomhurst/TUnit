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
    /// Registers a set of test entries for a class. The lightweight <paramref name="filterData"/>
    /// is held eagerly so discovery filtering can read names/categories/dependencies without JIT,
    /// while <paramref name="factory"/> is only invoked when an entry is actually selected for
    /// materialization — keeping the heavy per-class delegate/metadata <c>.cctor</c> off the
    /// discovery hot path.
    /// Returns a dummy value for use as a static field initializer.
    /// Multiple calls for the same T are additive — filter arrays and factories accumulate.
    /// </summary>
    public static int RegisterEntries<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] T>(TestEntryFilterData[] filterData, Func<TestEntry<T>[]> factory) where T : class
    {
        var key = typeof(T);

        while (true)
        {
            if (Sources.TestEntries.TryGetValue(key, out var existing))
            {
                if (existing is TestEntrySource<T> existingSource)
                {
                    existingSource.AddSource(filterData, factory);
                    return 0;
                }

                throw new InvalidOperationException(
                    $"Type mismatch in TestEntries for '{typeof(T).FullName}': expected TestEntrySource<{typeof(T).Name}>, found {existing.GetType().Name}");
            }

            if (Sources.TestEntries.TryAdd(key, new TestEntrySource<T>(filterData, factory)))
            {
                return 0;
            }

            // Another thread added between TryGetValue and TryAdd — retry to merge
        }
    }
}
