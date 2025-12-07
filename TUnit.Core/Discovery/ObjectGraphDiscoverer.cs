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

    // Flag to coordinate cache cleanup (prevents multiple threads cleaning simultaneously)
    private static int _cleanupInProgress;

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
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tracked objects dictionary (same as testContext.TrackedObjects).</returns>
    public ConcurrentDictionary<int, HashSet<object>> DiscoverAndTrackObjects(TestContext testContext, CancellationToken cancellationToken = default)
    {
        var visitedObjects = testContext.TrackedObjects;
        var testDetails = testContext.Metadata.TestDetails;

        foreach (var classArgument in testDetails.TestClassArguments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (classArgument != null && TryAddToHashSet(visitedObjects, 0, classArgument))
            {
                DiscoverNestedObjectsForTracking(classArgument, visitedObjects, 1, cancellationToken);
            }
        }

        foreach (var methodArgument in testDetails.TestMethodArguments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (methodArgument != null && TryAddToHashSet(visitedObjects, 0, methodArgument))
            {
                DiscoverNestedObjectsForTracking(methodArgument, visitedObjects, 1, cancellationToken);
            }
        }

        foreach (var property in testDetails.TestClassInjectedPropertyArguments.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (property != null && TryAddToHashSet(visitedObjects, 0, property))
            {
                DiscoverNestedObjectsForTracking(property, visitedObjects, 1, cancellationToken);
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
#if DEBUG
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
#endif
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
        int currentDepth,
        CancellationToken cancellationToken)
    {
        // Guard against excessive recursion to prevent stack overflow
        if (currentDepth > MaxRecursionDepth)
        {
#if DEBUG
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
#endif
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        // Check SourceRegistrar.IsEnabled for compatibility with existing TrackableObjectGraphProvider behavior
        if (!SourceRegistrar.IsEnabled)
        {
            foreach (var prop in plan.ReflectionProperties)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = prop.Property.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                if (!TryAddToHashSet(visitedObjects, currentDepth, value))
                {
                    continue;
                }

                DiscoverNestedObjectsForTracking(value, visitedObjects, currentDepth + 1, cancellationToken);
            }
        }
        else
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
                if (value == null)
                {
                    continue;
                }

                if (!TryAddToHashSet(visitedObjects, currentDepth, value))
                {
                    continue;
                }

                DiscoverNestedObjectsForTracking(value, visitedObjects, currentDepth + 1, cancellationToken);
            }
        }

        // Also discover nested IAsyncInitializer objects from ALL properties
        DiscoverNestedInitializerObjectsForTracking(obj, visitedObjects, currentDepth, cancellationToken);
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
#if DEBUG
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
#endif
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
#if DEBUG
                // Log instead of silently swallowing - helps with debugging
                Debug.WriteLine($"[ObjectGraphDiscoverer] Failed to access property '{property.Name}' on type '{type.Name}': {ex.Message}");
#endif
                // Continue discovery despite property access failures
                _ = ex;
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
        int currentDepth,
        CancellationToken cancellationToken)
    {
        // Guard against excessive recursion to prevent stack overflow
        if (currentDepth > MaxRecursionDepth)
        {
#if DEBUG
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
#endif
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var type = obj.GetType();

        if (ShouldSkipType(type))
        {
            return;
        }

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

                if (value is IAsyncInitializer && TryAddToHashSet(visitedObjects, currentDepth, value))
                {
                    DiscoverNestedObjectsForTracking(value, visitedObjects, currentDepth + 1, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[ObjectGraphDiscoverer] Failed to access property '{property.Name}' on type '{type.Name}': {ex.Message}");
#endif
                // Continue discovery despite property access failures
                _ = ex;
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
        // Use Interlocked to ensure only one thread performs cleanup at a time
        if (PropertyCache.Count > MaxCacheSize &&
            Interlocked.CompareExchange(ref _cleanupInProgress, 1, 0) == 0)
        {
            try
            {
                // Double-check after acquiring cleanup flag
                if (PropertyCache.Count > MaxCacheSize)
                {
                    var keysToRemove = new List<Type>(MaxCacheSize / 2);
                    var count = 0;
                    foreach (var key in PropertyCache.Keys)
                    {
                        if (count++ >= MaxCacheSize / 2)
                        {
                            break;
                        }

                        keysToRemove.Add(key);
                    }

                    foreach (var key in keysToRemove)
                    {
                        PropertyCache.TryRemove(key, out _);
                    }
#if DEBUG
                    Debug.WriteLine($"[ObjectGraphDiscoverer] PropertyCache exceeded {MaxCacheSize} entries, cleared {keysToRemove.Count} entries");
#endif
                }
            }
            finally
            {
                Interlocked.Exchange(ref _cleanupInProgress, 0);
            }
        }

        return PropertyCache.GetOrAdd(type, static t =>
        {
            // Use explicit loops instead of LINQ to avoid allocations in hot path
            var allProps = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // First pass: count eligible properties
            var count = 0;
            foreach (var p in allProps)
            {
                if (p.CanRead && p.GetIndexParameters().Length == 0)
                {
                    count++;
                }
            }

            // Second pass: fill result array
            var result = new PropertyInfo[count];
            var i = 0;
            foreach (var p in allProps)
            {
                if (p.CanRead && p.GetIndexParameters().Length == 0)
                {
                    result[i++] = p;
                }
            }

            return result;
        });
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
    /// Thread-safe: uses lock to protect HashSet modifications.
    /// </summary>
    private static void AddToDepth(ConcurrentDictionary<int, HashSet<object>> objectsByDepth, int depth, object obj)
    {
        var hashSet = objectsByDepth.GetOrAdd(depth, _ => new HashSet<object>(ReferenceComparer));
        lock (hashSet)
        {
            hashSet.Add(obj);
        }
    }

    /// <summary>
    /// Thread-safe add to HashSet at specified depth. Returns true if added (not duplicate).
    /// </summary>
    private static bool TryAddToHashSet(ConcurrentDictionary<int, HashSet<object>> dict, int depth, object obj)
    {
        var hashSet = dict.GetOrAdd(depth, _ => new HashSet<object>(ReferenceComparer));
        lock (hashSet)
        {
            return hashSet.Add(obj);
        }
    }
}
