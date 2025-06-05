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
    // Unified containers using the new improved architecture
    private static readonly ScopedContainer<string> GlobalContainer = new();
    private static readonly ScopedContainer<Type> ClassContainer = new();
    private static readonly ScopedContainer<Assembly> AssemblyContainer = new();
    private static readonly ScopedContainer<string> KeyContainer = new();

    // Improved dependency tracking
    private static readonly Data.DependencyTracker DependencyTracker = new();

    private static Disposer Disposer => new(GlobalContext.Current.GlobalLogger);    /// <summary>
    /// Gets an instance for the specified class.
    /// </summary>
    /// <param name="testClass">The test class type.</param>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetInstanceForClass(Type testClass, Type type, Func<object> func)
    {
        var scopedInstance = ClassContainer.GetOrCreate(testClass, type, func);
        return scopedInstance.Instance;
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
        var scopedInstance = AssemblyContainer.GetOrCreate(assembly, type, func);
        return scopedInstance.Instance;
    }

    /// <summary>
    /// Increments the global usage count for the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    public static void IncrementGlobalUsage(Type type)
    {
        if (GlobalContainer.TryGet(typeof(object).FullName!, type, out var instance))
        {
            instance?.UsageCount.Increment();
        }
    }

    /// <summary>
    /// Gets a global instance of the specified type.
    /// </summary>
    /// <param name="type">The type of object to retrieve</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static object GetGlobalInstance(Type type, Func<object> func)
    {
        var scopedInstance = GlobalContainer.GetOrCreate(typeof(object).FullName!, type, func);
        return scopedInstance.Instance;
    }

    /// <summary>
    /// Increments the usage count for the specified test class and type.
    /// </summary>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="type">The type.</param>
    public static void IncrementTestClassUsage(Type testClassType, Type type)
    {
        if (ClassContainer.TryGet(testClassType, type, out var instance))
        {
            instance?.UsageCount.Increment();
        }
    }

    /// <summary>
    /// Increments the usage count for the specified assembly and type.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="type">The type.</param>
    public static void IncrementAssemblyUsage(Assembly assembly, Type type)
    {
        if (AssemblyContainer.TryGet(assembly, type, out var instance))
        {
            instance?.UsageCount.Increment();
        }
    }

    /// <summary>
    /// Increments the usage count for the specified key and type.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    public static void IncrementKeyUsage(string key, Type type)
    {
        if (KeyContainer.TryGet(key, type, out var instance))
        {
            instance?.UsageCount.Increment();
        }
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
        var scopedInstance = KeyContainer.GetOrCreate(key, type, func);
        return scopedInstance.Instance;
    }    /// <summary>
    /// Consumes the count for the specified key and type.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    internal static async ValueTask ConsumeKey(string key, Type type)
    {
        if (!KeyContainer.TryGet(key, type, out var instance) || instance == null)
        {
            // Log warning about untracked object disposal
            GlobalContext.Current.GlobalLogger?.LogWarning($"Attempting to dispose untracked key-scoped object of type {type.Name} with key {key}");
            return;
        }

        if (instance.UsageCount.Decrement() > 0)
        {
            return;
        }

        var removedInstance = KeyContainer.Remove(key, type);
        if (removedInstance != null)
        {
            await DisposeWithNestedDependencies(removedInstance.Instance);
        }
    }

    /// <summary>
    /// Consumes the global count for the specified item with improved error handling.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeGlobalCount<T>(T? item)
    {
        if (!GlobalContainer.TryGet(typeof(object).FullName!, typeof(T), out var instance) || instance == null)
        {
            // Log warning about untracked object disposal
            GlobalContext.Current.GlobalLogger?.LogWarning($"Attempting to dispose untracked global object of type {typeof(T).Name}");
            await Disposer.DisposeAsync(item);
            return;
        }

        if (instance.UsageCount.Decrement() > 0)
        {
            return;
        }

        var removedInstance = GlobalContainer.Remove(typeof(object).FullName!, typeof(T));
        if (removedInstance != null)
        {
            await DisposeWithNestedDependencies(removedInstance.Instance);
        }
    }

    /// <summary>
    /// Consumes the assembly count for the specified item with improved error handling.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="assembly">The assembly.</param>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeAssemblyCount<T>(Assembly assembly, T? item)
    {
        if (!AssemblyContainer.TryGet(assembly, typeof(T), out var instance) || instance == null)
        {
            // Log warning about untracked object disposal
            GlobalContext.Current.GlobalLogger?.LogWarning($"Attempting to dispose untracked assembly-scoped object of type {typeof(T).Name} in assembly {assembly.GetName().Name}");
            await Disposer.DisposeAsync(item);
            return;
        }

        if (instance.UsageCount.Decrement() > 0)
        {
            return;
        }

        var removedInstance = AssemblyContainer.Remove(assembly, typeof(T));
        if (removedInstance != null)
        {
            await DisposeWithNestedDependencies(removedInstance.Instance);
        }
    }

    /// <summary>
    /// Consumes the test class count for the specified item with improved error handling.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeTestClassCount<T>(Type testClassType, T? item)
    {
        if (!ClassContainer.TryGet(testClassType, typeof(T), out var instance) || instance == null)
        {
            // Log warning about untracked object disposal
            GlobalContext.Current.GlobalLogger?.LogWarning($"Attempting to dispose untracked class-scoped object of type {typeof(T).Name} in class {testClassType.Name}");
            await Disposer.DisposeAsync(item);
            return;
        }

        if (instance.UsageCount.Decrement() > 0)
        {
            return;
        }

        var removedInstance = ClassContainer.Remove(testClassType, typeof(T));
        if (removedInstance != null)
        {
            await DisposeWithNestedDependencies(removedInstance.Instance);
        }
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
    }    /// <summary>
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
            NestedDependencies = DependencyTracker.GetDependencyCount(),
            Details = new Dictionary<string, object>
            {
                ["Global"] = globalDiagnostics,
                ["Class"] = classDiagnostics,
                ["Assembly"] = assemblyDiagnostics,
                ["Key"] = keyDiagnostics,
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

        sb.AppendLine($"Total Dependencies Tracked: {metrics.NestedDependencies}");
        sb.AppendLine($"Total Instances: {metrics.TotalInstances}");

        return sb.ToString();
    }    /// <summary>
    /// Disposes an object and all its nested dependencies using the new dependency tracker.
    /// </summary>
    /// <param name="item">The item to dispose.</param>
    private static async ValueTask DisposeWithNestedDependencies<T>(T? item)
    {
        if (item is null)
        {
            return;
        }

        // First, dispose nested dependencies using the new dependency tracker
        await DependencyTracker.DisposeNestedDependenciesAsync(item);

        // Then dispose the item itself
        await Disposer.DisposeAsync(item);
    }    /// <summary>
    /// Registers a nested dependency relationship between a parent and child object using the new dependency tracker.
    /// </summary>
    /// <param name="parentObject">The parent object that depends on the child.</param>
    /// <param name="childObject">The child object that the parent depends on.</param>
    /// <param name="childSharedType">The shared type of the child object.</param>
    /// <param name="childKey">The key of the child object (for keyed sharing).</param>
    internal static void RegisterNestedDependency(object parentObject, object childObject, SharedType childSharedType, string? childKey)
    {
        // For now, we'll track the dependency relationship directly without scope managers
        // since the current scope managers don't have a unified interface that supports all scenarios
        // The new dependency tracker will handle disposal ordering properly

        // Create a simple disposable reference that just disposes the child directly
        var disposableRef = new DirectDisposableReference(childObject);
        DependencyTracker.RegisterDependency(parentObject, childObject, new DirectScopeManager());

        // Don't increment usage counts here - they're already managed by the main ClassDataSource attribute logic
        // We only track the relationship for proper disposal ordering
    }

    /// <summary>
    /// A simple scope manager that directly disposes objects without additional scope logic.
    /// This is used for nested dependency tracking where we just want disposal ordering.
    /// </summary>
    private class DirectScopeManager : IScopeManager
    {
        public T GetOrCreate<T>(Func<T> factory) => factory();
        public void IncrementUsage<T>() { /* No-op for direct disposal */ }
        public Task<bool> TryDisposeAsync<T>(T item)
        {
            // Always dispose directly since this is for nested dependencies
            return DisposeHelper.DisposeAsync(item).AsTask().ContinueWith(_ => true);
        }
    }

    /// <summary>
    /// A direct disposable reference implementation.
    /// </summary>
    private class DirectDisposableReference
    {
        private readonly object _instance;

        public DirectDisposableReference(object instance)
        {
            _instance = instance;
        }

        public async Task DisposeAsync()
        {
            await DisposeHelper.DisposeAsync(_instance);
        }
    }

    /// <summary>
    /// Helper class for disposing objects (duplicate of the one in GlobalScopeManager for internal use).
    /// </summary>
    private static class DisposeHelper
    {
        public static async ValueTask DisposeAsync(object? obj)
        {
            switch (obj)
            {
                case null:
                    return;
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }
}
