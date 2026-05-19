using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Hooks;
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
    private static readonly ConcurrentDictionary<Assembly, byte> ExcludedAssemblies = new();
    private static readonly ConcurrentDictionary<string, byte> ExcludedAssemblyNames = new();

    public static bool IsEnabled { get; set; }

    public static void ExcludeAssemblyFromDiscovery(string assemblyName)
    {
        ExcludedAssemblyNames.TryAdd(assemblyName, 0);
    }

    public static void ExcludeAssemblyFromDiscovery(Assembly assembly)
    {
        ExcludedAssemblies.TryAdd(assembly, 0);
    }

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
        if (IsAssemblyExcludedFromDiscovery(testSource.GetType().Assembly))
        {
            return;
        }

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
    /// Registers a hook factory into a type-keyed dictionary. The factory is not invoked
    /// until the engine materializes the hook for execution. Use a <c>static</c> lambda
    /// (no captures) to keep module-init cost minimal and to remain AOT compatible.
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    public static int RegisterHook<T>(ConcurrentDictionary<Type, ConcurrentBag<LazyHookEntry<T>>> dictionary, Type key, int registrationIndex, Func<int, T> factory)
        where T : HookMethod
    {
        if (IsAssemblyExcludedFromDiscovery(key.Assembly))
        {
            return 0;
        }

        dictionary.GetOrAdd(key, static _ => new ConcurrentBag<LazyHookEntry<T>>())
            .Add(new LazyHookEntry<T>(registrationIndex, factory));
        return 0;
    }

    /// <summary>
    /// Registers a hook factory into an assembly-keyed dictionary. The factory is not invoked
    /// until the engine materializes the hook for execution. Use a <c>static</c> lambda
    /// (no captures) to keep module-init cost minimal and to remain AOT compatible.
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    public static int RegisterHook<T>(ConcurrentDictionary<Assembly, ConcurrentBag<LazyHookEntry<T>>> dictionary, Assembly key, int registrationIndex, Func<int, T> factory)
        where T : HookMethod
    {
        if (IsAssemblyExcludedFromDiscovery(key))
        {
            return 0;
        }

        dictionary.GetOrAdd(key, static _ => new ConcurrentBag<LazyHookEntry<T>>())
            .Add(new LazyHookEntry<T>(registrationIndex, factory));
        return 0;
    }

    /// <summary>
    /// Registers a hook factory into a global bag. The factory is not invoked until the engine
    /// materializes the hook for execution. Use a <c>static</c> lambda (no captures) to keep
    /// module-init cost minimal and to remain AOT compatible.
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int RegisterHook<T>(ConcurrentBag<LazyHookEntry<T>> bag, int registrationIndex, Func<int, T> factory)
        where T : HookMethod
    {
        if (IsAssemblyExcludedFromDiscovery(Assembly.GetCallingAssembly()))
        {
            return 0;
        }

        bag.Add(new LazyHookEntry<T>(registrationIndex, factory));
        return 0;
    }

    /// <summary>
    /// Registers a factory for test entries. The factory is not invoked until the engine
    /// needs to access the entries (during discovery/filtering), avoiding per-class JIT
    /// compilation during module initialization.
    /// Returns a dummy value for use as a static field initializer.
    /// Multiple calls for the same T are additive — factories accumulate.
    /// </summary>
    public static int RegisterEntries<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] T>(Func<TestEntry<T>[]> factory) where T : class
    {
        var key = typeof(T);

        if (IsAssemblyExcludedFromDiscovery(key.Assembly))
        {
            return 0;
        }

        while (true)
        {
            if (Sources.TestEntries.TryGetValue(key, out var existing))
            {
                if (existing is TestEntrySource<T> existingSource)
                {
                    existingSource.AddFactory(factory);
                    return 0;
                }

                throw new InvalidOperationException(
                    $"Type mismatch in TestEntries for '{typeof(T).FullName}': expected TestEntrySource<{typeof(T).Name}>, found {existing.GetType().Name}");
            }

            if (Sources.TestEntries.TryAdd(key, new TestEntrySource<T>(factory)))
            {
                return 0;
            }

            // Another thread added between TryGetValue and TryAdd — retry to merge
        }
    }

    internal static bool IsAssemblyExcludedFromDiscovery(Assembly assembly)
    {
        if (ExcludedAssemblies.ContainsKey(assembly))
        {
            return true;
        }

        var assemblyName = assembly.GetName().Name;
        if (assemblyName is not null && ExcludedAssemblyNames.ContainsKey(assemblyName))
        {
            return true;
        }

        if (HasSelfExclusionAttribute(assembly))
        {
            return true;
        }

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null || ReferenceEquals(entryAssembly, assembly))
        {
            return false;
        }

        return EntryAssemblyExcludes(entryAssembly, assembly);
    }

    private static bool HasSelfExclusionAttribute(Assembly assembly)
    {
        try
        {
            foreach (var attribute in CustomAttributeData.GetCustomAttributes(assembly))
            {
                if (IsExcludeFromDiscoveryAttribute(attribute) && attribute.ConstructorArguments.Count == 0)
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool EntryAssemblyExcludes(Assembly entryAssembly, Assembly assembly)
    {
        try
        {
            foreach (var attribute in CustomAttributeData.GetCustomAttributes(entryAssembly))
            {
                if (!IsExcludeFromDiscoveryAttribute(attribute))
                {
                    continue;
                }

                foreach (var markerType in GetAssemblyMarkerTypes(attribute))
                {
                    if (AssemblyName.ReferenceMatchesDefinition(markerType.Assembly.GetName(), assembly.GetName()))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsExcludeFromDiscoveryAttribute(CustomAttributeData attribute)
    {
        return attribute.AttributeType == typeof(ExcludeFromTestDiscoveryAttribute) ||
               attribute.AttributeType.FullName == typeof(ExcludeFromTestDiscoveryAttribute).FullName;
    }

    private static IEnumerable<Type> GetAssemblyMarkerTypes(CustomAttributeData attribute)
    {
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.ArgumentType == typeof(Type) && argument.Value is Type type)
            {
                yield return type;
                continue;
            }

            if (!argument.ArgumentType.IsArray || argument.Value is not IEnumerable<CustomAttributeTypedArgument> values)
            {
                continue;
            }

            foreach (var value in values)
            {
                if (value.Value is Type markerType)
                {
                    yield return markerType;
                }
            }
        }
    }
}
