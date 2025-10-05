using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.PropertyInjection;
using TUnit.Core.StaticProperties;

namespace TUnit.Core.Tracking;

internal class TrackableObjectGraphProvider
{
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Trackable object discovery uses reflection for property injection")]
    #endif
    public ConcurrentDictionary<int, HashSet<object>> GetTrackableObjects(TestContext testContext)
    {
        var visitedObjects = testContext.TrackedObjects;

        var testDetails = testContext.TestDetails;

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

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Nested object tracking uses reflection for property discovery")]
    #endif
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

                if (!PropertyInjectionCache.HasInjectableProperties(value.GetType()))
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

                if (!PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    continue;
                }

                AddNestedTrackableObjects(value, visitedObjects, currentDepth + 1);
            }
        }
    }
}
