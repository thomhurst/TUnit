using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;

namespace TUnit.Core.Discovery;

/// <summary>
/// Centralized service for discovering and organizing object graphs.
/// Consolidates duplicate graph traversal logic from ObjectGraphDiscoveryService and TrackableObjectGraphProvider.
/// Follows Single Responsibility Principle - only discovers objects, doesn't modify them.
/// </summary>
/// <remarks>
/// <para>
/// This class is thread-safe and uses cached reflection for performance.
/// Objects are organized by their nesting depth in the hierarchy:
/// </para>
/// <list type="bullet">
/// <item><description>Depth 0: Root objects (class args, method args, property values)</description></item>
/// <item><description>Depth 1+: Nested objects found in properties of objects at previous depth</description></item>
/// </list>
/// </remarks>
public sealed class ObjectGraphDiscoverer : IObjectGraphDiscoverer
{
    /// <summary>
    /// Maximum recursion depth for object graph discovery.
    /// Prevents stack overflow on deep or circular object graphs.
    /// </summary>
    private const int MaxRecursionDepth = 50;

    /// <summary>
    /// Maximum size for the property cache before cleanup is triggered.
    /// Prevents unbounded memory growth in long-running test sessions.
    /// </summary>
    private const int MaxCacheSize = 10000;

    // Cache for GetProperties() results per type - eliminates repeated reflection calls
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    // Reference equality comparer for object tracking (ignores Equals overrides)
    private static readonly Helpers.ReferenceEqualityComparer ReferenceComparer = new();

    // Types to skip during discovery (primitives, strings, system types)
    private static readonly HashSet<Type> SkipTypes =
    [
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
    ];

    /// <inheritdoc />
    public IObjectGraph DiscoverObjectGraph(TestContext testContext, CancellationToken cancellationToken = default)
    {
        var objectsByDepth = new ConcurrentDictionary<int, HashSet<object>>();
        var allObjects = new HashSet<object>();
        // Use ConcurrentDictionary for thread-safe visited tracking with reference equality
        var visitedObjects = new ConcurrentDictionary<object, byte>(ReferenceComparer);

        var testDetails = testContext.Metadata.TestDetails;

        // Collect root-level objects (depth 0)
        foreach (var classArgument in testDetails.TestClassArguments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (classArgument != null && visitedObjects.TryAdd(classArgument, 0))
            {
                AddToDepth(objectsByDepth, 0, classArgument);
                allObjects.Add(classArgument);
                DiscoverNestedObjects(classArgument, objectsByDepth, visitedObjects, allObjects, currentDepth: 1, cancellationToken);
            }
        }

        foreach (var methodArgument in testDetails.TestMethodArguments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (methodArgument != null && visitedObjects.TryAdd(methodArgument, 0))
            {
                AddToDepth(objectsByDepth, 0, methodArgument);
                allObjects.Add(methodArgument);
                DiscoverNestedObjects(methodArgument, objectsByDepth, visitedObjects, allObjects, currentDepth: 1, cancellationToken);
            }
        }

        foreach (var property in testDetails.TestClassInjectedPropertyArguments.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (property != null && visitedObjects.TryAdd(property, 0))
            {
                AddToDepth(objectsByDepth, 0, property);
                allObjects.Add(property);
                DiscoverNestedObjects(property, objectsByDepth, visitedObjects, allObjects, currentDepth: 1, cancellationToken);
            }
        }

        return new ObjectGraph(objectsByDepth, allObjects);
    }

    /// <inheritdoc />
    public IObjectGraph DiscoverNestedObjectGraph(object rootObject, CancellationToken cancellationToken = default)
    {
        var objectsByDepth = new ConcurrentDictionary<int, HashSet<object>>();
        var allObjects = new HashSet<object>();
        var visitedObjects = new ConcurrentDictionary<object, byte>(ReferenceComparer);

        if (visitedObjects.TryAdd(rootObject, 0))
        {
            AddToDepth(objectsByDepth, 0, rootObject);
            allObjects.Add(rootObject);
            DiscoverNestedObjects(rootObject, objectsByDepth, visitedObjects, allObjects, currentDepth: 1, cancellationToken);
        }

        return new ObjectGraph(objectsByDepth, allObjects);
    }

    /// <summary>
    /// Discovers objects and adds them to the existing tracked objects dictionary.
    /// Used by TrackableObjectGraphProvider to populate TestContext.TrackedObjects.
    /// </summary>
    /// <param name="testContext">The test context to discover objects from.</param>
    /// <returns>The tracked objects dictionary (same as testContext.TrackedObjects).</returns>
    public ConcurrentDictionary<int, HashSet<object>> DiscoverAndTrackObjects(TestContext testContext)
    {
        var visitedObjects = testContext.TrackedObjects;
        var testDetails = testContext.Metadata.TestDetails;

        foreach (var classArgument in testDetails.TestClassArguments)
        {
            if (classArgument != null && GetOrAddHashSet(visitedObjects, 0).Add(classArgument))
            {
                DiscoverNestedObjectsForTracking(classArgument, visitedObjects, 1);
            }
        }

        foreach (var methodArgument in testDetails.TestMethodArguments)
        {
            if (methodArgument != null && GetOrAddHashSet(visitedObjects, 0).Add(methodArgument))
            {
                DiscoverNestedObjectsForTracking(methodArgument, visitedObjects, 1);
            }
        }

        foreach (var property in testDetails.TestClassInjectedPropertyArguments.Values)
        {
            if (property != null && GetOrAddHashSet(visitedObjects, 0).Add(property))
            {
                DiscoverNestedObjectsForTracking(property, visitedObjects, 1);
            }
        }

        return visitedObjects;
    }

    /// <summary>
    /// Recursively discovers nested objects that have injectable properties OR implement IAsyncInitializer.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property discovery handles both AOT and reflection modes")]
    private void DiscoverNestedObjects(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        ConcurrentDictionary<object, byte> visitedObjects,
        HashSet<object> allObjects,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        // Guard against excessive recursion to prevent stack overflow
        if (currentDepth > MaxRecursionDepth)
        {
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        // First, discover objects from injectable properties (data source attributes)
        if (plan.HasProperties)
        {
            // Use source-generated properties if available, otherwise fall back to reflection
            if (plan.SourceGeneratedProperties.Length > 0)
            {
                foreach (var metadata in plan.SourceGeneratedProperties)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
                    if (property == null || !property.CanRead)
                    {
                        continue;
                    }

                    var value = property.GetValue(obj);
                    if (value == null || !visitedObjects.TryAdd(value, 0))
                    {
                        continue;
                    }

                    AddToDepth(objectsByDepth, currentDepth, value);
                    allObjects.Add(value);
                    DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, currentDepth + 1, cancellationToken);
                }
            }
            else if (plan.ReflectionProperties.Length > 0)
            {
                foreach (var (property, _) in plan.ReflectionProperties)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var value = property.GetValue(obj);
                    if (value == null || !visitedObjects.TryAdd(value, 0))
                    {
                        continue;
                    }

                    AddToDepth(objectsByDepth, currentDepth, value);
                    allObjects.Add(value);
                    DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, currentDepth + 1, cancellationToken);
                }
            }
        }

        // Also discover nested IAsyncInitializer objects from ALL properties
        DiscoverNestedInitializerObjects(obj, objectsByDepth, visitedObjects, allObjects, currentDepth, cancellationToken);
    }

    /// <summary>
    /// Discovers nested objects for tracking (uses HashSet pattern for compatibility with TestContext.TrackedObjects).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property discovery handles both AOT and reflection modes")]
    private void DiscoverNestedObjectsForTracking(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> visitedObjects,
        int currentDepth)
    {
        // Guard against excessive recursion to prevent stack overflow
        if (currentDepth > MaxRecursionDepth)
        {
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
            return;
        }

        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        // Check SourceRegistrar.IsEnabled for compatibility with existing TrackableObjectGraphProvider behavior
        if (!SourceRegistrar.IsEnabled)
        {
            foreach (var prop in plan.ReflectionProperties)
            {
                var value = prop.Property.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                if (!GetOrAddHashSet(visitedObjects, currentDepth).Add(value))
                {
                    continue;
                }

                DiscoverNestedObjectsForTracking(value, visitedObjects, currentDepth + 1);
            }
        }
        else
        {
            foreach (var metadata in plan.SourceGeneratedProperties)
            {
                var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
                if (property == null || !property.CanRead)
                {
                    continue;
                }

                var value = property.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                if (!GetOrAddHashSet(visitedObjects, currentDepth).Add(value))
                {
                    continue;
                }

                DiscoverNestedObjectsForTracking(value, visitedObjects, currentDepth + 1);
            }
        }

        // Also discover nested IAsyncInitializer objects from ALL properties
        DiscoverNestedInitializerObjectsForTracking(obj, visitedObjects, currentDepth);
    }

    /// <summary>
    /// Discovers nested objects that implement IAsyncInitializer from all readable properties.
    /// Uses cached reflection for performance.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    private void DiscoverNestedInitializerObjects(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        ConcurrentDictionary<object, byte> visitedObjects,
        HashSet<object> allObjects,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        // Guard against excessive recursion to prevent stack overflow
        if (currentDepth > MaxRecursionDepth)
        {
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var type = obj.GetType();

        // Skip types that don't need discovery
        if (ShouldSkipType(type))
        {
            return;
        }

        // Use cached properties for performance
        var properties = GetCachedProperties(type);

        foreach (var property in properties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var value = property.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                // Only discover if it implements IAsyncInitializer and hasn't been visited
                if (value is IAsyncInitializer && visitedObjects.TryAdd(value, 0))
                {
                    AddToDepth(objectsByDepth, currentDepth, value);
                    allObjects.Add(value);
                    DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, currentDepth + 1, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
                // Log instead of silently swallowing - helps with debugging
                Debug.WriteLine($"[ObjectGraphDiscoverer] Failed to access property '{property.Name}' on type '{type.Name}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Discovers nested IAsyncInitializer objects for tracking (uses HashSet pattern).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    private void DiscoverNestedInitializerObjectsForTracking(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> visitedObjects,
        int currentDepth)
    {
        // Guard against excessive recursion to prevent stack overflow
        if (currentDepth > MaxRecursionDepth)
        {
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
            return;
        }

        var type = obj.GetType();

        if (ShouldSkipType(type))
        {
            return;
        }

        var properties = GetCachedProperties(type);

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                if (value is IAsyncInitializer && GetOrAddHashSet(visitedObjects, currentDepth).Add(value))
                {
                    DiscoverNestedObjectsForTracking(value, visitedObjects, currentDepth + 1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ObjectGraphDiscoverer] Failed to access property '{property.Name}' on type '{type.Name}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets cached properties for a type, filtering to only readable non-indexed properties.
    /// Includes periodic cache cleanup to prevent unbounded memory growth.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    private static PropertyInfo[] GetCachedProperties(Type type)
    {
        // Periodic cleanup if cache grows too large to prevent memory leaks
        if (PropertyCache.Count > MaxCacheSize)
        {
            // Clear half the cache (simple approach - non-LRU for performance)
            var keysToRemove = PropertyCache.Keys.Take(MaxCacheSize / 2).ToList();
            foreach (var key in keysToRemove)
            {
                PropertyCache.TryRemove(key, out _);
            }

            Debug.WriteLine($"[ObjectGraphDiscoverer] PropertyCache exceeded {MaxCacheSize} entries, cleared {keysToRemove.Count} entries");
        }

        return PropertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
             .ToArray());
    }

    /// <summary>
    /// Clears the property cache. Called at end of test session to release memory.
    /// </summary>
    public static void ClearCache()
    {
        PropertyCache.Clear();
    }

    /// <summary>
    /// Checks if a type should be skipped during discovery.
    /// </summary>
    private static bool ShouldSkipType(Type type)
    {
        return type.IsPrimitive ||
               SkipTypes.Contains(type) ||
               type.Namespace?.StartsWith("System") == true;
    }

    /// <summary>
    /// Adds an object to the specified depth level.
    /// </summary>
    private static void AddToDepth(ConcurrentDictionary<int, HashSet<object>> objectsByDepth, int depth, object obj)
    {
        objectsByDepth.GetOrAdd(depth, _ => []).Add(obj);
    }

    /// <summary>
    /// Gets or creates a HashSet at the specified depth (thread-safe).
    /// </summary>
    private static HashSet<object> GetOrAddHashSet(ConcurrentDictionary<int, HashSet<object>> dict, int depth)
    {
        return dict.GetOrAdd(depth, _ => []);
    }
}
