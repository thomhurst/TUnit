using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.PropertyInjection;

namespace TUnit.Core.Lifecycle;

/// <summary>
/// Unified implementation of object graph traversal for TUnit.
/// Consolidates the duplicate traversal logic from:
/// - DataSourceInitializer.CollectNestedObjects()
/// - PropertyInjectionService.RecurseIntoNestedPropertiesAsync()
/// - TrackableObjectGraphProvider.AddNestedTrackableObjects()
/// </summary>
internal sealed class ObjectGraphWalker : IObjectGraphWalker
{
    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyCollection<object>> WalkGraph(
        object root,
        Func<object, bool>? filter = null)
    {
        var objectsByDepth = new ConcurrentDictionary<int, HashSet<object>>();
        var visitedObjects = new HashSet<object>();

        // Add root at depth 0
        if (ShouldInclude(root, filter))
        {
            objectsByDepth.GetOrAdd(0, _ => []).Add(root);
            visitedObjects.Add(root);
        }

        // Collect nested objects
        CollectNestedObjects(root, objectsByDepth, visitedObjects, 1, filter);

        // Convert to readonly collections
        return objectsByDepth.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyCollection<object>)kvp.Value.ToList().AsReadOnly());
    }

    /// <inheritdoc />
    public async ValueTask WalkGraphAsync(
        object root,
        Func<object, int, ValueTask> visitor,
        Func<object, bool>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var objectsByDepth = WalkGraph(root, filter);

        // Visit in depth-first order (deepest first) for proper initialization ordering
        var depths = objectsByDepth.Keys.OrderByDescending(d => d);

        foreach (var depth in depths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var objectsAtDepth = objectsByDepth[depth];

            // Process objects at this depth in parallel
            var tasks = objectsAtDepth.Select(obj => visitor(obj, depth).AsTask());
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void CollectObjects(
        object root,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        int startDepth = 0)
    {
        // Add root at the specified depth if not already visited
        if (visitedObjects.Add(root))
        {
            objectsByDepth.GetOrAdd(startDepth, _ => []).Add(root);
            CollectNestedObjects(root, objectsByDepth, visitedObjects, startDepth + 1, null);
        }
    }

    /// <summary>
    /// Recursively collects nested objects that have injectable properties.
    /// Handles both source-generated and reflection modes.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Property injection cache handles both AOT and reflection modes appropriately")]
    private void CollectNestedObjects(
        object obj,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        int currentDepth,
        Func<object, bool>? filter)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        // Handle source-generated properties
        if (plan.SourceGeneratedProperties.Length > 0)
        {
            foreach (var metadata in plan.SourceGeneratedProperties)
            {
                ProcessProperty(
                    obj,
                    metadata.ContainingType,
                    metadata.PropertyName,
                    objectsByDepth,
                    visitedObjects,
                    currentDepth,
                    filter);
            }
        }
        // Handle reflection-based properties
        else if (plan.ReflectionProperties.Length > 0)
        {
            foreach (var (property, _) in plan.ReflectionProperties)
            {
                var value = property.GetValue(obj);
                if (value != null && visitedObjects.Add(value))
                {
                    ProcessNestedValue(value, objectsByDepth, visitedObjects, currentDepth, filter);
                }
            }
        }
    }

    /// <summary>
    /// Processes a property to extract and track its value.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Members annotated with 'DynamicallyAccessedMembersAttribute' require dynamic access",
        Justification = "Property metadata comes from source generator or is cached")]
    private void ProcessProperty(
        object obj,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type containingType,
        string propertyName,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        int currentDepth,
        Func<object, bool>? filter)
    {
        var property = containingType.GetProperty(propertyName);
        if (property == null || !property.CanRead)
        {
            return;
        }

        var value = property.GetValue(obj);
        if (value == null || !visitedObjects.Add(value))
        {
            return;
        }

        ProcessNestedValue(value, objectsByDepth, visitedObjects, currentDepth, filter);
    }

    /// <summary>
    /// Processes a nested value, adding it to the collection and recursing if needed.
    /// </summary>
    private void ProcessNestedValue(
        object value,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        int currentDepth,
        Func<object, bool>? filter)
    {
        // Check if this object should be included
        if (!ShouldInclude(value, filter))
        {
            return;
        }

        // Add to the current depth level if it has injectable properties or is IAsyncInitializer
        if (PropertyInjectionCache.HasInjectableProperties(value.GetType()) ||
            value is Interfaces.IAsyncInitializer)
        {
            objectsByDepth.GetOrAdd(currentDepth, _ => []).Add(value);
        }

        // Recursively collect nested objects if it has injectable properties
        if (PropertyInjectionCache.HasInjectableProperties(value.GetType()))
        {
            CollectNestedObjects(value, objectsByDepth, visitedObjects, currentDepth + 1, filter);
        }
    }

    /// <summary>
    /// Determines if an object should be included based on the filter.
    /// </summary>
    private static bool ShouldInclude(object obj, Func<object, bool>? filter)
    {
        return filter == null || filter(obj);
    }
}
