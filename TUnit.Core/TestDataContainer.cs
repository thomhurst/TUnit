using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Logging;

namespace TUnit.Core;

/// <summary>
/// Represents a container for test data with improved error handling and unified data structures.
/// </summary>
internal static class TestDataContainer
{
    // New unified containers
    private static readonly ScopedContainer<string> GlobalContainer = new();
    private static readonly ScopedContainer<Type> ClassContainer = new();
    private static readonly ScopedContainer<Assembly> AssemblyContainer = new();    private static readonly ScopedContainer<string> KeyContainer = new();    // Improved dependency tracking
    private static readonly Data.DependencyTracker DependencyTracker = new();

    // Legacy containers for backward compatibility (will be phased out)
    private static readonly GetOnlyDictionary<Type, object> InjectedSharedGlobally = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<Type, object>> InjectedSharedPerClassType = new();
    private static readonly GetOnlyDictionary<Assembly, GetOnlyDictionary<Type, object>> InjectedSharedPerAssembly = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<string, object>> InjectedSharedPerKey = new();

    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Counter>> CountsPerKey = new();
    private static readonly ConcurrentDictionary<Type, Counter> CountsPerTestSession = new();
    private static readonly ConcurrentDictionary<Assembly, ConcurrentDictionary<Type, Counter>> CountsPerAssembly = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Counter>> CountsPerTestClass = new();

    // Track nested dependencies: parent object -> list of (child object, child shared type, child key)
    private static readonly ConcurrentDictionary<object, List<(object child, SharedType sharedType, string key)>> NestedDependencies = new();

    private static Disposer Disposer => new(GlobalContext.Current.GlobalLogger);

    /// <summary>
    /// Gets an instance for the specified class.
    /// </summary>
    /// <param name="testClass">The test class type.</param>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetInstanceForClass(Type testClass, Type type, Func<object> func)
    {
        var objectsForClass = InjectedSharedPerClassType.GetOrAdd(testClass, _ => new GetOnlyDictionary<Type, object>());

        return objectsForClass.GetOrAdd(type, _ => func());
    }

    /// <summary>
    /// Gets an instance for the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetInstanceForAssembly(Assembly assembly, Type type, Func<object> func)
    {
        var objectsForClass = InjectedSharedPerAssembly.GetOrAdd(assembly, _ => new GetOnlyDictionary<Type, object>());

        return  objectsForClass.GetOrAdd(type, _ => func());
    }

    /// <summary>
    /// Increments the global usage count for the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    public static void IncrementGlobalUsage(Type type)
    {
        CountsPerTestSession.GetOrAdd(type, _ => new Counter()).Increment();
    }

    /// <summary>
    /// Gets a global instance of the specified type.
    /// </summary>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetGlobalInstance(Type type, Func<object> func)
    {
        return InjectedSharedGlobally.GetOrAdd(type, _ => func());
    }

    /// <summary>
    /// Increments the usage count for the specified test class and type.
    /// </summary>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="type">The type.</param>
    public static void IncrementTestClassUsage(Type testClassType, Type type)
    {
        var itemsForTestClass = CountsPerTestClass.GetOrAdd(testClassType, _ => []);

        itemsForTestClass.GetOrAdd(type, _ => new Counter()).Increment();
    }

    /// <summary>
    /// Increments the usage count for the specified assembly and type.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="type">The type.</param>
    public static void IncrementAssemblyUsage(Assembly assembly, Type type)
    {
        var itemsForAssembly = CountsPerAssembly.GetOrAdd(assembly, _ => []);

        itemsForAssembly.GetOrAdd(type, _ => new Counter()).Increment();
    }

    /// <summary>
    /// Increments the usage count for the specified key and type.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    public static void IncrementKeyUsage(string key, Type type)
    {
        var keysForType = CountsPerKey.GetOrAdd(type, _ => []);

        keysForType.GetOrAdd(key, _ => new Counter()).Increment();
    }

    /// <summary>
    /// Gets an instance for the specified key.
    /// </summary>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="key">The key.</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetInstanceForKey(string key, Type type, Func<object> func)
    {
        var instancesForType = InjectedSharedPerKey.GetOrAdd(type, _ => new GetOnlyDictionary<string, object>());

        return instancesForType.GetOrAdd(key, _ => func());
    }      /// <summary>
    /// Consumes the count for the specified key and type.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    internal static async ValueTask ConsumeKey(string key, Type type)
    {
        var keysForType = CountsPerKey[type];

        if (keysForType[key].Decrement() > 0)
        {
            return;
        }

        var instancesForType = InjectedSharedPerKey.GetOrAdd(type, _ => new GetOnlyDictionary<string, object>());

        await DisposeWithNestedDependencies(instancesForType.Remove(key));
    }/// <summary>
    /// Consumes the global count for the specified item with improved error handling.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeGlobalCount<T>(T? item)
    {
        if (!CountsPerTestSession.TryGetValue(typeof(T), out var counter))
        {
            // Log warning about untracked object disposal
            GlobalContext.Current.GlobalLogger?.LogWarning($"Attempting to dispose untracked global object of type {typeof(T).Name}");
            await Disposer.DisposeAsync(item);
            return;
        }

        if (counter.Decrement() > 0)
        {
            return;
        }

        await DisposeWithNestedDependencies(item);
    }
      /// <summary>
    /// Consumes the assembly count for the specified item with improved error handling.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="assembly">The assembly.</param>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeAssemblyCount<T>(Assembly assembly, T? item)
    {
        if (!CountsPerAssembly.TryGetValue(assembly, out var assemblyCounters) ||
            !assemblyCounters.TryGetValue(typeof(T), out var counter))
        {
            // Log warning about untracked object disposal
            GlobalContext.Current.GlobalLogger?.LogWarning($"Attempting to dispose untracked assembly-scoped object of type {typeof(T).Name} in assembly {assembly.GetName().Name}");
            await Disposer.DisposeAsync(item);
            return;
        }

        if (counter.Decrement() > 0)
        {
            return;
        }

        await DisposeWithNestedDependencies(item);
    }

    /// <summary>
    /// Consumes the test class count for the specified item with improved error handling.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeTestClassCount<T>(Type testClassType, T? item)
    {
        if (!CountsPerTestClass.TryGetValue(testClassType, out var classCounters) ||
            !classCounters.TryGetValue(typeof(T), out var counter))
        {
            // Log warning about untracked object disposal
            GlobalContext.Current.GlobalLogger?.LogWarning($"Attempting to dispose untracked class-scoped object of type {typeof(T).Name} in class {testClassType.Name}");
            await Disposer.DisposeAsync(item);
            return;
        }

        if (counter.Decrement() > 0)
        {
            return;
        }

        await DisposeWithNestedDependencies(item);
    }    /// <summary>
    /// Gets an improved instance for the specified key using the unified container approach.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static T GetKeyedInstance<T>(string key, Func<T> func) where T : class
    {
        var scopedInstance = KeyContainer.GetOrCreate(key, typeof(T), () => func()!);
        return (T)scopedInstance.Instance;
    }

    /// <summary>
    /// Gets an improved global instance using the unified container approach.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve</typeparam>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static T GetImprovedGlobalInstance<T>(Func<T> func) where T : class
    {
        var scopedInstance = GlobalContainer.GetOrCreate(typeof(T).FullName!, typeof(T), () => func()!);
        return (T)scopedInstance.Instance;
    }

    /// <summary>
    /// Gets an improved class instance using the unified container approach.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve</typeparam>
    /// <param name="testClass">The test class type.</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static T GetImprovedClassInstance<T>(Type testClass, Func<T> func) where T : class
    {
        var scopedInstance = ClassContainer.GetOrCreate(testClass, typeof(T), () => func()!);
        return (T)scopedInstance.Instance;
    }

    /// <summary>
    /// Gets an improved assembly instance using the unified container approach.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve</typeparam>
    /// <param name="assembly">The assembly.</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static T GetImprovedAssemblyInstance<T>(Assembly assembly, Func<T> func) where T : class
    {
        var scopedInstance = AssemblyContainer.GetOrCreate(assembly, typeof(T), () => func()!);
        return (T)scopedInstance.Instance;
    }

    /// <summary>
    /// Consumes the count for the specified key and type with improved error handling.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    internal static async ValueTask ConsumeKeyImproved(string key, Type type)
    {
        var scopedInstance = KeyContainer.Remove(key, type);
        if (scopedInstance != null)
        {
            await DependencyTracker.DisposeNestedDependenciesAsync(scopedInstance.Instance);
            await Disposer.DisposeAsync(scopedInstance.Instance);
        }
    }

    /// <summary>
    /// Gets diagnostic metrics for the test data container.
    /// </summary>
    /// <returns>The metrics information.</returns>
    public static TestDataMetrics GetMetrics()
    {
        var globalDiagnostics = GlobalContainer.GetDiagnostics();
        var classDiagnostics = ClassContainer.GetDiagnostics();
        var assemblyDiagnostics = AssemblyContainer.GetDiagnostics();
        var keyDiagnostics = KeyContainer.GetDiagnostics();

        return new TestDataMetrics
        {
            GlobalInstances = (int)globalDiagnostics["TotalInstances"],
            ClassScopedInstances = (int)classDiagnostics["TotalInstances"],
            AssemblyScopedInstances = (int)assemblyDiagnostics["TotalInstances"],
            KeyScopedInstances = (int)keyDiagnostics["TotalInstances"],
            NestedDependencies = NestedDependencies.Count,
            Details = new Dictionary<string, object>
            {
                ["Global"] = globalDiagnostics,
                ["Class"] = classDiagnostics,
                ["Assembly"] = assemblyDiagnostics,
                ["Key"] = keyDiagnostics,
                ["LegacyCounts"] = new Dictionary<string, int>
                {
                    ["Global"] = CountsPerTestSession.Count,
                    ["Class"] = CountsPerTestClass.Count,
                    ["Assembly"] = CountsPerAssembly.Count,
                    ["Key"] = CountsPerKey.Count
                }
            }
        };
    }

    /// <summary>
    /// Dumps diagnostic information about the current state of the container.
    /// </summary>
    /// <returns>A string containing diagnostic information.</returns>
    public static string DumpDiagnostics()
    {
        var metrics = GetMetrics();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("=== TestDataContainer Diagnostics ===");
        sb.AppendLine($"Timestamp: {DateTimeOffset.UtcNow}");
        sb.AppendLine();

        sb.AppendLine("New Unified Containers:");
        sb.AppendLine($"  Global Instances: {metrics.GlobalInstances}");
        sb.AppendLine($"  Class Instances: {metrics.ClassScopedInstances}");
        sb.AppendLine($"  Assembly Instances: {metrics.AssemblyScopedInstances}");
        sb.AppendLine($"  Keyed Instances: {metrics.KeyScopedInstances}");
        sb.AppendLine();

        sb.AppendLine("Legacy Containers (backward compatibility):");
        var legacyCounts = (Dictionary<string, int>)metrics.Details["LegacyCounts"];
        sb.AppendLine($"  Global: {legacyCounts["Global"]}");
        sb.AppendLine($"  Class: {legacyCounts["Class"]}");
        sb.AppendLine($"  Assembly: {legacyCounts["Assembly"]}");
        sb.AppendLine($"  Keyed: {legacyCounts["Key"]}");
        sb.AppendLine();

        sb.AppendLine($"Total Dependencies Tracked: {metrics.NestedDependencies}");
        sb.AppendLine($"Total Instances: {metrics.TotalInstances}");

        return sb.ToString();
    }

    /// <summary>
    /// Disposes an object and all its nested dependencies using the legacy approach.
    /// </summary>
    /// <param name="item">The item to dispose.</param>
    private static async ValueTask DisposeWithNestedDependencies<T>(T? item)
    {
        if (item is null)
        {
            return;
        }

        // First, dispose nested dependencies using legacy approach
        if (NestedDependencies.TryRemove(item, out var dependencies))
        {
            foreach (var (child, sharedType, key) in dependencies)
            {
                switch (sharedType)
                {
                    case SharedType.PerTestSession:
                        await ConsumeGlobalCount(child);
                        break;
                    case SharedType.PerAssembly:
                        await ConsumeAssemblyCount(child.GetType().Assembly, child);
                        break;
                    case SharedType.Keyed:
                        await ConsumeKey(key, child.GetType());
                        break;
                    case SharedType.PerClass:
                        // For class-scoped dependencies, we need the test class type
                        // This is a limitation of the legacy approach - we'll use the improved approach going forward
                        await Disposer.DisposeAsync(child);
                        break;
                    case SharedType.None:
                        await Disposer.DisposeAsync(child);
                        break;
                }
            }
        }

        // Then dispose the item itself
        await Disposer.DisposeAsync(item);
    }

    /// <summary>
    /// Registers a nested dependency relationship between a parent and child object (legacy approach).
    /// </summary>
    /// <param name="parentObject">The parent object that depends on the child.</param>
    /// <param name="childObject">The child object that the parent depends on.</param>
    /// <param name="childSharedType">The shared type of the child object.</param>
    /// <param name="childKey">The key of the child object (for keyed sharing).</param>
    internal static void RegisterNestedDependency(object parentObject, object childObject, SharedType childSharedType, string childKey)
    {
        var dependencies = NestedDependencies.GetOrAdd(parentObject, _ => new List<(object, SharedType, string)>());

        lock (dependencies)
        {
            dependencies.Add((childObject, childSharedType, childKey));
        }

        // Don't increment usage counts here - they're already managed by the main ClassDataSource attribute logic
        // We only track the relationship for proper disposal ordering
    }
}
