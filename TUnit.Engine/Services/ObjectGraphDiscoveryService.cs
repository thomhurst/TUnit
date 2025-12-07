using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;

namespace TUnit.Engine.Services;

/// <summary>
/// Centralized service for discovering and organizing object graphs.
/// Eliminates duplicate graph traversal logic that was scattered across
/// PropertyInjectionService, DataSourceInitializer, and TrackableObjectGraphProvider.
/// Follows Single Responsibility Principle - only discovers objects, doesn't modify them.
/// </summary>
internal sealed class ObjectGraphDiscoveryService
{
    /// <summary>
    /// Discovers all objects from test context arguments and properties, organized by depth level.
    /// Depth 0 = root objects (class args, method args, property values)
    /// Depth 1+ = nested objects found in properties of objects at previous depth
    /// </summary>
    public ObjectGraph DiscoverObjectGraph(TestContext testContext)
    {
        var objectsByDepth = new ConcurrentDictionary<int, HashSet<object>>();
        var allObjects = new HashSet<object>();
        var visitedObjects = new HashSet<object>();

        var testDetails = testContext.Metadata.TestDetails;

        // Collect root-level objects (depth 0)
        foreach (var classArgument in testDetails.TestClassArguments)
        {
            if (classArgument != null && visitedObjects.Add(classArgument))
            {
                AddToDepth(objectsByDepth, 0, classArgument);
                allObjects.Add(classArgument);
                DiscoverNestedObjects(classArgument, objectsByDepth, visitedObjects, allObjects, currentDepth: 1);
            }
        }

        foreach (var methodArgument in testDetails.TestMethodArguments)
        {
            if (methodArgument != null && visitedObjects.Add(methodArgument))
            {
                AddToDepth(objectsByDepth, 0, methodArgument);
                allObjects.Add(methodArgument);
                DiscoverNestedObjects(methodArgument, objectsByDepth, visitedObjects, allObjects, currentDepth: 1);
            }
        }

        foreach (var property in testDetails.TestClassInjectedPropertyArguments.Values)
        {
            if (property != null && visitedObjects.Add(property))
            {
                AddToDepth(objectsByDepth, 0, property);
                allObjects.Add(property);
                DiscoverNestedObjects(property, objectsByDepth, visitedObjects, allObjects, currentDepth: 1);
            }
        }

        return new ObjectGraph(objectsByDepth, allObjects);
    }

    /// <summary>
    /// Discovers nested objects from a single root object, organized by depth.
    /// Used for discovering objects within a data source or property value.
    /// </summary>
    public ObjectGraph DiscoverNestedObjectGraph(object rootObject)
    {
        var objectsByDepth = new ConcurrentDictionary<int, HashSet<object>>();
        var allObjects = new HashSet<object>();
        var visitedObjects = new HashSet<object>();

        if (visitedObjects.Add(rootObject))
        {
            AddToDepth(objectsByDepth, 0, rootObject);
            allObjects.Add(rootObject);
            DiscoverNestedObjects(rootObject, objectsByDepth, visitedObjects, allObjects, currentDepth: 1);
        }

        return new ObjectGraph(objectsByDepth, allObjects);
    }

    /// <summary>
    /// Recursively discovers nested objects that have injectable properties OR implement IAsyncInitializer.
    /// This ensures that all nested objects that need initialization are discovered,
    /// even if they don't have explicit data source attributes.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property discovery handles both AOT and reflection modes")]
    private void DiscoverNestedObjects(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        HashSet<object> allObjects,
        int currentDepth)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        // First, discover objects from injectable properties (data source attributes)
        if (plan.HasProperties)
        {
            // Use source-generated properties if available, otherwise fall back to reflection
            if (plan.SourceGeneratedProperties.Length > 0)
            {
                foreach (var metadata in plan.SourceGeneratedProperties)
                {
                    var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
                    if (property == null || !property.CanRead)
                    {
                        continue;
                    }

                    var value = property.GetValue(obj);
                    if (value == null || !visitedObjects.Add(value))
                    {
                        continue;
                    }

                    AddToDepth(objectsByDepth, currentDepth, value);
                    allObjects.Add(value);

                    // Recursively discover nested objects
                    DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, currentDepth + 1);
                }
            }
            else if (plan.ReflectionProperties.Length > 0)
            {
                foreach (var (property, _) in plan.ReflectionProperties)
                {
                    var value = property.GetValue(obj);
                    if (value == null || !visitedObjects.Add(value))
                    {
                        continue;
                    }

                    AddToDepth(objectsByDepth, currentDepth, value);
                    allObjects.Add(value);

                    // Recursively discover nested objects
                    DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, currentDepth + 1);
                }
            }
        }

        // Also discover nested IAsyncInitializer objects from ALL properties
        // This handles cases where nested objects don't have data source attributes
        // but still implement IAsyncInitializer and need to be initialized
        DiscoverNestedInitializerObjects(obj, objectsByDepth, visitedObjects, allObjects, currentDepth);
    }

    /// <summary>
    /// Discovers nested objects that implement IAsyncInitializer from all readable properties.
    /// This is separate from injectable property discovery to handle objects without data source attributes.
    /// This is a best-effort fallback - in AOT scenarios, properties with data source attributes are discovered via source generation.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    private void DiscoverNestedInitializerObjects(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        HashSet<object> allObjects,
        int currentDepth)
    {
        var type = obj.GetType();

        // Skip primitive types, strings, and system types
        if (type.IsPrimitive || type == typeof(string) || type.Namespace?.StartsWith("System") == true)
        {
            return;
        }

        // Get all readable instance properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
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

                // Only discover if it implements IAsyncInitializer and hasn't been visited
                if (value is IAsyncInitializer && visitedObjects.Add(value))
                {
                    AddToDepth(objectsByDepth, currentDepth, value);
                    allObjects.Add(value);

                    // Recursively discover nested objects
                    DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, currentDepth + 1);
                }
            }
            catch
            {
                // Ignore properties that throw exceptions when accessed
            }
        }
    }

    private static void AddToDepth(ConcurrentDictionary<int, HashSet<object>> objectsByDepth, int depth, object obj)
    {
        objectsByDepth.GetOrAdd(depth, _ => []).Add(obj);
    }
}

/// <summary>
/// Represents a discovered object graph organized by depth level.
/// </summary>
internal sealed class ObjectGraph
{
    public ObjectGraph(ConcurrentDictionary<int, HashSet<object>> objectsByDepth, HashSet<object> allObjects)
    {
        ObjectsByDepth = objectsByDepth;
        AllObjects = allObjects;
        MaxDepth = objectsByDepth.Count > 0 ? objectsByDepth.Keys.Max() : -1;
    }

    /// <summary>
    /// Objects organized by depth (0 = root arguments, 1+ = nested).
    /// </summary>
    public ConcurrentDictionary<int, HashSet<object>> ObjectsByDepth { get; }

    /// <summary>
    /// All unique objects in the graph.
    /// </summary>
    public HashSet<object> AllObjects { get; }

    /// <summary>
    /// Maximum nesting depth (-1 if empty).
    /// </summary>
    public int MaxDepth { get; }

    /// <summary>
    /// Gets objects at a specific depth level.
    /// </summary>
    public IEnumerable<object> GetObjectsAtDepth(int depth)
    {
        return ObjectsByDepth.TryGetValue(depth, out var objects) ? objects : [];
    }

    /// <summary>
    /// Gets depth levels in descending order (deepest first).
    /// </summary>
    public IEnumerable<int> GetDepthsDescending()
    {
        return ObjectsByDepth.Keys.OrderByDescending(d => d);
    }
}
