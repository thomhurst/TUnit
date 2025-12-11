using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.PropertyInjection;

namespace TUnit.Core.Discovery;

/// <summary>
/// Represents an error that occurred during object graph discovery.
/// </summary>
/// <param name="TypeName">The name of the type being inspected.</param>
/// <param name="PropertyName">The name of the property that failed to access.</param>
/// <param name="ErrorMessage">The error message.</param>
/// <param name="Exception">The exception that occurred.</param>
internal readonly record struct DiscoveryError(string TypeName, string PropertyName, string ErrorMessage, Exception Exception);

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
/// <para>
/// Discovery errors (e.g., property access failures) are collected in <see cref="GetDiscoveryErrors"/>
/// rather than thrown, allowing discovery to continue despite individual property failures.
/// </para>
/// </remarks>
internal sealed class ObjectGraphDiscoverer : IObjectGraphTracker
{
    /// <summary>
    /// Maximum recursion depth for object graph discovery.
    /// Prevents stack overflow on deep or circular object graphs.
    /// </summary>
    private const int MaxRecursionDepth = 50;

    // Reference equality comparer for object tracking (ignores Equals overrides)
    private static readonly Helpers.ReferenceEqualityComparer ReferenceComparer = Helpers.ReferenceEqualityComparer.Instance;

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

    // Thread-safe collection of discovery errors for diagnostics
    private static readonly ConcurrentBag<DiscoveryError> DiscoveryErrors = [];

    /// <summary>
    /// Gets all discovery errors that occurred during object graph traversal.
    /// Useful for debugging and diagnostics when property access fails.
    /// </summary>
    /// <returns>A read-only list of discovery errors.</returns>
    public static IReadOnlyList<DiscoveryError> GetDiscoveryErrors()
    {
        return DiscoveryErrors.ToArray();
    }

    /// <summary>
    /// Clears all recorded discovery errors. Call at end of test session.
    /// </summary>
    public static void ClearDiscoveryErrors()
    {
        DiscoveryErrors.Clear();
    }

    /// <summary>
    /// Delegate for adding discovered objects to collections.
    /// Returns true if the object was newly added (not a duplicate).
    /// </summary>
    private delegate bool TryAddObjectFunc(object obj, int depth);

    /// <summary>
    /// Delegate for recursive discovery after an object is added.
    /// </summary>
    private delegate void RecurseFunc(object obj, int depth);

    /// <summary>
    /// Delegate for processing a root object after it's been added.
    /// </summary>
    private delegate void RootObjectCallback(object obj);

    /// <inheritdoc />
    public IObjectGraph DiscoverObjectGraph(TestContext testContext, CancellationToken cancellationToken = default)
    {
        var objectsByDepth = new ConcurrentDictionary<int, HashSet<object>>();
        var allObjects = new HashSet<object>(ReferenceComparer);
        var allObjectsLock = new object(); // Thread-safety for allObjects HashSet
        var visitedObjects = new ConcurrentDictionary<object, byte>(ReferenceComparer);

        // Standard mode add callback (thread-safe)
        bool TryAddStandard(object obj, int depth)
        {
            if (!visitedObjects.TryAdd(obj, 0))
            {
                return false;
            }

            AddToDepth(objectsByDepth, depth, obj);
            lock (allObjectsLock)
            {
                allObjects.Add(obj);
            }

            return true;
        }

        // Collect root-level objects and discover nested objects
        CollectRootObjects(
            testContext.Metadata.TestDetails,
            TryAddStandard,
            obj => DiscoverNestedObjects(obj, objectsByDepth, visitedObjects, allObjects, allObjectsLock, currentDepth: 1, cancellationToken),
            cancellationToken);

        return new ObjectGraph(objectsByDepth, allObjects);
    }

    /// <inheritdoc />
    public IObjectGraph DiscoverNestedObjectGraph(object rootObject, CancellationToken cancellationToken = default)
    {
        var objectsByDepth = new ConcurrentDictionary<int, HashSet<object>>();
        var allObjects = new HashSet<object>(ReferenceComparer);
        var allObjectsLock = new object(); // Thread-safety for allObjects HashSet
        var visitedObjects = new ConcurrentDictionary<object, byte>(ReferenceComparer);

        if (visitedObjects.TryAdd(rootObject, 0))
        {
            AddToDepth(objectsByDepth, 0, rootObject);
            lock (allObjectsLock)
            {
                allObjects.Add(rootObject);
            }

            DiscoverNestedObjects(rootObject, objectsByDepth, visitedObjects, allObjects, allObjectsLock, currentDepth: 1, cancellationToken);
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

        // Collect root-level objects and discover nested objects for tracking
        CollectRootObjects(
            testContext.Metadata.TestDetails,
            (obj, depth) => TryAddToHashSet(visitedObjects, depth, obj),
            obj => DiscoverNestedObjectsForTracking(obj, visitedObjects, 1, cancellationToken),
            cancellationToken);

        return visitedObjects;
    }

    /// <summary>
    /// Recursively discovers nested objects that have injectable properties OR implement IAsyncInitializer.
    /// Uses consolidated TraverseInjectableProperties and TraverseInitializerProperties methods.
    /// </summary>
    private void DiscoverNestedObjects(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        ConcurrentDictionary<object, byte> visitedObjects,
        HashSet<object> allObjects,
        object allObjectsLock,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        if (!CheckRecursionDepth(obj, currentDepth))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Standard mode add callback: visitedObjects + objectsByDepth + allObjects (thread-safe)
        bool TryAddStandard(object value, int depth)
        {
            if (!visitedObjects.TryAdd(value, 0))
            {
                return false;
            }

            AddToDepth(objectsByDepth, depth, value);
            lock (allObjectsLock)
            {
                allObjects.Add(value);
            }

            return true;
        }

        // Recursive callback
        void Recurse(object value, int depth)
        {
            DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, allObjectsLock, depth, cancellationToken);
        }

        // Traverse injectable properties (useSourceRegistrarCheck = false)
        TraverseInjectableProperties(obj, TryAddStandard, Recurse, currentDepth, cancellationToken, useSourceRegistrarCheck: false);

        // Also discover nested IAsyncInitializer objects from ALL properties
        TraverseInitializerProperties(obj, TryAddStandard, Recurse, currentDepth, cancellationToken);
    }

    /// <summary>
    /// Discovers nested objects for tracking (uses HashSet pattern for compatibility with TestContext.TrackedObjects).
    /// Uses consolidated TraverseInjectableProperties and TraverseInitializerProperties methods.
    /// </summary>
    private void DiscoverNestedObjectsForTracking(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> visitedObjects,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        if (!CheckRecursionDepth(obj, currentDepth))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Tracking mode add callback: TryAddToHashSet only
        bool TryAddTracking(object value, int depth)
        {
            return TryAddToHashSet(visitedObjects, depth, value);
        }

        // Recursive callback
        void Recurse(object value, int depth)
        {
            DiscoverNestedObjectsForTracking(value, visitedObjects, depth, cancellationToken);
        }

        // Traverse injectable properties (useSourceRegistrarCheck = true for tracking mode)
        TraverseInjectableProperties(obj, TryAddTracking, Recurse, currentDepth, cancellationToken, useSourceRegistrarCheck: true);

        // Also discover nested IAsyncInitializer objects from ALL properties
        TraverseInitializerProperties(obj, TryAddTracking, Recurse, currentDepth, cancellationToken);
    }

    /// <summary>
    /// Clears all caches. Called at end of test session to release memory.
    /// </summary>
    public static void ClearCache()
    {
        PropertyCacheManager.ClearCache();
        ClearDiscoveryErrors();
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

    #region Consolidated Traversal Methods (DRY)

    /// <summary>
    /// Checks recursion depth guard. Returns false if depth exceeded (caller should return early).
    /// </summary>
    private static bool CheckRecursionDepth(object obj, int currentDepth)
    {
        if (currentDepth > MaxRecursionDepth)
        {
#if DEBUG
            Debug.WriteLine($"[ObjectGraphDiscoverer] Max recursion depth ({MaxRecursionDepth}) reached for type '{obj.GetType().Name}'");
#endif
            return false;
        }

        return true;
    }

    /// <summary>
    /// Unified traversal for injectable properties (from PropertyInjectionCache).
    /// Eliminates duplicate code between DiscoverNestedObjects and DiscoverNestedObjectsForTracking.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property discovery handles both AOT and reflection modes")]
    private static void TraverseInjectableProperties(
        object obj,
        TryAddObjectFunc tryAdd,
        RecurseFunc recurse,
        int currentDepth,
        CancellationToken cancellationToken,
        bool useSourceRegistrarCheck)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        if (!plan.HasProperties && !useSourceRegistrarCheck)
        {
            return;
        }

        // The two modes differ in how they choose source-gen vs reflection:
        // - Standard mode: Uses plan.SourceGeneratedProperties.Length > 0
        // - Tracking mode: Uses SourceRegistrar.IsEnabled
        bool useSourceGen = useSourceRegistrarCheck
            ? SourceRegistrar.IsEnabled
            : plan.SourceGeneratedProperties.Length > 0;

        if (useSourceGen)
        {
            TraverseSourceGeneratedProperties(obj, plan.SourceGeneratedProperties, tryAdd, recurse, currentDepth, cancellationToken);
        }
        else
        {
            var reflectionProps = useSourceRegistrarCheck
                ? plan.ReflectionProperties
                : (plan.ReflectionProperties.Length > 0 ? plan.ReflectionProperties : []);

            TraverseReflectionProperties(obj, reflectionProps, tryAdd, recurse, currentDepth, cancellationToken);
        }
    }

    /// <summary>
    /// Traverses source-generated properties and discovers nested objects.
    /// Extracted for reduced complexity in TraverseInjectableProperties.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property discovery handles both AOT and reflection modes")]
    private static void TraverseSourceGeneratedProperties(
        object obj,
        PropertyInjectionMetadata[] sourceGeneratedProperties,
        TryAddObjectFunc tryAdd,
        RecurseFunc recurse,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        foreach (var metadata in sourceGeneratedProperties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
            if (property == null || !property.CanRead)
            {
                continue;
            }

            var value = property.GetValue(obj);
            if (value != null && tryAdd(value, currentDepth))
            {
                recurse(value, currentDepth + 1);
            }
        }
    }

    /// <summary>
    /// Traverses reflection-based properties and discovers nested objects.
    /// Extracted for reduced complexity in TraverseInjectableProperties.
    /// </summary>
    private static void TraverseReflectionProperties(
        object obj,
        (PropertyInfo Property, IDataSourceAttribute DataSource)[] reflectionProperties,
        TryAddObjectFunc tryAdd,
        RecurseFunc recurse,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        foreach (var prop in reflectionProperties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = prop.Property.GetValue(obj);
            if (value != null && tryAdd(value, currentDepth))
            {
                recurse(value, currentDepth + 1);
            }
        }
    }

    /// <summary>
    /// Unified traversal for IAsyncInitializer objects (from all properties).
    /// Eliminates duplicate code between DiscoverNestedInitializerObjects and DiscoverNestedInitializerObjectsForTracking.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    private static void TraverseInitializerProperties(
        object obj,
        TryAddObjectFunc tryAdd,
        RecurseFunc recurse,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        var type = obj.GetType();

        if (ShouldSkipType(type))
        {
            return;
        }

        var properties = PropertyCacheManager.GetCachedProperties(type);

        foreach (var property in properties)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Only access properties whose declared type could be IAsyncInitializer.
            // This prevents triggering side effects from unrelated property getters
            // (e.g., WebApplicationFactory.Server which starts the test host when accessed).
            // Properties typed as object/interfaces not extending IAsyncInitializer won't be
            // checked - users should properly type their properties or use data source attributes.
            if (!typeof(IAsyncInitializer).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            try
            {
                var value = property.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                // Only discover IAsyncInitializer objects
                if (value is IAsyncInitializer && tryAdd(value, currentDepth))
                {
                    recurse(value, currentDepth + 1);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
                // Record error for diagnostics (available via GetDiscoveryErrors())
                DiscoveryErrors.Add(new DiscoveryError(type.Name, property.Name, ex.Message, ex));
#if DEBUG
                Debug.WriteLine($"[ObjectGraphDiscoverer] Failed to access property '{property.Name}' on type '{type.Name}': {ex.Message}");
#endif
                // Continue discovery despite property access failures
            }
        }
    }

    /// <summary>
    /// Collects root-level objects (class args, method args, properties) from test details.
    /// Eliminates duplicate loops in DiscoverObjectGraph and DiscoverAndTrackObjects.
    /// </summary>
    /// <remarks>
    /// For injected properties, only DIRECT test class properties (including inherited) are added at depth 0.
    /// Nested properties (properties of injected objects) are discovered through normal
    /// graph traversal at appropriate depths (1+), ensuring correct initialization order
    /// for nested IAsyncInitializer dependencies. See GitHub issue #4032.
    /// </remarks>
    private static void CollectRootObjects(
        TestDetails testDetails,
        TryAddObjectFunc tryAdd,
        RootObjectCallback onRootObjectAdded,
        CancellationToken cancellationToken)
    {
        // Process class arguments
        ProcessRootCollection(testDetails.TestClassArguments, tryAdd, onRootObjectAdded, cancellationToken);

        // Process method arguments
        ProcessRootCollection(testDetails.TestMethodArguments, tryAdd, onRootObjectAdded, cancellationToken);

        // Build set of types in the test class hierarchy (for identifying direct properties)
        var hierarchyTypes = GetTypeHierarchy(testDetails.ClassType);

        // Process ONLY direct test class injected properties at depth 0.
        // Nested properties will be discovered through normal graph traversal at depth 1+.
        // This ensures proper initialization order for nested IAsyncInitializer dependencies.
        foreach (var kvp in testDetails.TestClassInjectedPropertyArguments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (kvp.Value == null)
            {
                continue;
            }

            // Check if this property belongs to the test class hierarchy (not nested object properties)
            // Cache key format: "{DeclaringType.FullName}.{PropertyName}"
            if (IsDirectProperty(kvp.Key, hierarchyTypes))
            {
                if (tryAdd(kvp.Value, 0))
                {
                    onRootObjectAdded(kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Gets all types in the inheritance hierarchy from the given type up to (but not including) object.
    /// </summary>
    private static HashSet<string> GetTypeHierarchy(Type type)
    {
        var result = new HashSet<string>();
        var currentType = type;

        while (currentType != null && currentType != typeof(object))
        {
            if (currentType.FullName != null)
            {
                result.Add(currentType.FullName);
            }

            currentType = currentType.BaseType;
        }

        return result;
    }

    /// <summary>
    /// Determines if a cache key represents a direct property (belonging to test class hierarchy)
    /// vs a nested property (belonging to an injected object).
    /// Cache key format: "{DeclaringType.FullName}.{PropertyName}"
    /// </summary>
    private static bool IsDirectProperty(string cacheKey, HashSet<string> hierarchyTypes)
    {
        // Find the last dot to separate type from property name
        var lastDotIndex = cacheKey.LastIndexOf('.');
        if (lastDotIndex <= 0)
        {
            return true; // Malformed key, treat as direct
        }

        var declaringTypeName = cacheKey.Substring(0, lastDotIndex);
        return hierarchyTypes.Contains(declaringTypeName);
    }

    /// <summary>
    /// Processes a collection of root objects, adding them to the graph and invoking callback.
    /// Extracted to eliminate duplicate iteration patterns in CollectRootObjects.
    /// </summary>
    private static void ProcessRootCollection<T>(
        IEnumerable<T> collection,
        TryAddObjectFunc tryAdd,
        RootObjectCallback onRootObjectAdded,
        CancellationToken cancellationToken)
    {
        foreach (var item in collection)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item != null && tryAdd(item, 0))
            {
                onRootObjectAdded(item);
            }
        }
    }

    #endregion
}
