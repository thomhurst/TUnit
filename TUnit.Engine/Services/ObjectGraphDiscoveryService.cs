using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
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
    /// Recursively discovers nested objects that have injectable properties.
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

        if (!plan.HasProperties)
        {
            return;
        }

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

                // Recursively discover if this value has injectable properties
                if (PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, currentDepth + 1);
                }
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

                // Recursively discover if this value has injectable properties
                if (PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    DiscoverNestedObjects(value, objectsByDepth, visitedObjects, allObjects, currentDepth + 1);
                }
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
