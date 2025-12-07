using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;
using TUnit.Core.StaticProperties;

namespace TUnit.Core.Tracking;

internal class TrackableObjectGraphProvider
{
    public ConcurrentDictionary<int, HashSet<object>> GetTrackableObjects(TestContext testContext)
    {
        var visitedObjects = testContext.TrackedObjects;

        var testDetails = testContext.Metadata.TestDetails;

        foreach (var classArgument in testDetails.TestClassArguments)
        {
            if (classArgument != null && visitedObjects.GetOrAdd(0, []).Add(classArgument))
            {
                AddNestedTrackableObjects(classArgument, visitedObjects, 1);
            }
        }

        foreach (var methodArgument in testDetails.TestMethodArguments)
        {
            if (methodArgument != null && visitedObjects.GetOrAdd(0, []).Add(methodArgument))
            {
                AddNestedTrackableObjects(methodArgument, visitedObjects, 1);
            }
        }

        foreach (var property in testDetails.TestClassInjectedPropertyArguments.Values)
        {
            if (property != null && visitedObjects.GetOrAdd(0, []).Add(property))
            {
                AddNestedTrackableObjects(property, visitedObjects, 1);
            }
        }

        return visitedObjects;
    }

    private static void AddToLevel(Dictionary<int, List<object>> objectsByLevel, int level, object obj)
    {
        if (!objectsByLevel.TryGetValue(level, out var list))
        {
            list = [];
            objectsByLevel[level] = list;
        }
        list.Add(obj);
    }

    /// <summary>
    /// Get trackable objects for static properties (session-level)
    /// </summary>
    public IEnumerable<object> GetStaticPropertyTrackableObjects()
    {
        foreach (var value in StaticPropertyRegistry.GetAllInitializedValues())
        {
            if (value != null)
            {
                yield return value;
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property discovery handles both AOT and reflection modes")]
    private void AddNestedTrackableObjects(object obj, ConcurrentDictionary<int, HashSet<object>> visitedObjects, int currentDepth)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        if(!SourceRegistrar.IsEnabled)
        {
            foreach (var prop in plan.ReflectionProperties)
            {
                var value = prop.Property.GetValue(obj);

                if (value == null)
                {
                    continue;
                }

                // Check if already visited before yielding to prevent duplicates
                if (!visitedObjects.GetOrAdd(currentDepth, []).Add(value))
                {
                    continue;
                }

                AddNestedTrackableObjects(value, visitedObjects, currentDepth + 1);
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

                // Check if already visited before yielding to prevent duplicates
                if (!visitedObjects.GetOrAdd(currentDepth, []).Add(value))
                {
                    continue;
                }

                AddNestedTrackableObjects(value, visitedObjects, currentDepth + 1);
            }
        }

        // Also discover nested IAsyncInitializer objects from ALL properties
        // This handles cases where nested objects don't have data source attributes
        // but still implement IAsyncInitializer and need to be tracked for disposal
        AddNestedInitializerObjects(obj, visitedObjects, currentDepth);
    }

    /// <summary>
    /// Discovers nested objects that implement IAsyncInitializer from all readable properties.
    /// This is separate from injectable property discovery to handle objects without data source attributes.
    /// This is a best-effort fallback - in AOT scenarios, properties with data source attributes are discovered via source generation.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    private void AddNestedInitializerObjects(object obj, ConcurrentDictionary<int, HashSet<object>> visitedObjects, int currentDepth)
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
                if (value is IAsyncInitializer && visitedObjects.GetOrAdd(currentDepth, []).Add(value))
                {
                    // Recursively discover nested objects
                    AddNestedTrackableObjects(value, visitedObjects, currentDepth + 1);
                }
            }
            catch
            {
                // Ignore properties that throw exceptions when accessed
            }
        }
    }
}
