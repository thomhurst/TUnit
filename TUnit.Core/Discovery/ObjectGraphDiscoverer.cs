using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;

namespace TUnit.Core.Discovery;

/// <summary>
/// Represents an error that occurred during object graph discovery.
/// </summary>
/// <param name="TypeName">The name of the type being inspected.</param>
/// <param name="PropertyName">The name of the property that failed to access.</param>
/// <param name="ErrorMessage">The error message.</param>
/// <param name="Exception">The exception that occurred.</param>
public readonly record struct DiscoveryError(string TypeName, string PropertyName, string ErrorMessage, Exception Exception);

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
public sealed class ObjectGraphDiscoverer : IObjectGraphTracker
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
            foreach (var metadata in plan.SourceGeneratedProperties)
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
        else
        {
            // Reflection path - use the appropriate property collection
            var reflectionProps = useSourceRegistrarCheck
                ? plan.ReflectionProperties
                : (plan.ReflectionProperties.Length > 0 ? plan.ReflectionProperties : []);

            foreach (var prop in reflectionProps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = prop.Property.GetValue(obj);
                if (value != null && tryAdd(value, currentDepth))
                {
                    recurse(value, currentDepth + 1);
                }
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
    private static void CollectRootObjects(
        TestDetails testDetails,
        TryAddObjectFunc tryAdd,
        RootObjectCallback onRootObjectAdded,
        CancellationToken cancellationToken)
    {
        foreach (var classArgument in testDetails.TestClassArguments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (classArgument != null && tryAdd(classArgument, 0))
            {
                onRootObjectAdded(classArgument);
            }
        }

        foreach (var methodArgument in testDetails.TestMethodArguments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (methodArgument != null && tryAdd(methodArgument, 0))
            {
                onRootObjectAdded(methodArgument);
            }
        }

        foreach (var property in testDetails.TestClassInjectedPropertyArguments.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (property != null && tryAdd(property, 0))
            {
                onRootObjectAdded(property);
            }
        }
    }

    #endregion
}
