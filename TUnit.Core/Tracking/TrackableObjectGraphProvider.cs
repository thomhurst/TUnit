using TUnit.Core.PropertyInjection;
using TUnit.Core.StaticProperties;

namespace TUnit.Core.Tracking;

internal class TrackableObjectGraphProvider
{
    public IEnumerable<object> GetTrackableObjects(TestContext testContext, HashSet<object> visitedObjects)
    {
        var testDetails = testContext.TestDetails;

        foreach (var classArgument in testDetails.TestClassArguments)
        {
            if (classArgument != null && visitedObjects.Add(classArgument))
            {
                yield return classArgument;

                foreach (var nested in GetNestedTrackableObjects(classArgument, visitedObjects))
                {
                    yield return nested;
                }
            }
        }

        foreach (var methodArgument in testDetails.TestMethodArguments)
        {
            if (methodArgument != null && visitedObjects.Add(methodArgument))
            {
                yield return methodArgument;

                foreach (var nested in GetNestedTrackableObjects(methodArgument, visitedObjects))
                {
                    yield return nested;
                }
            }
        }

        foreach (var property in testDetails.TestClassInjectedPropertyArguments.Values)
        {
            if (property != null && visitedObjects.Add(property))
            {
                yield return property;

                foreach (var nested in GetNestedTrackableObjects(property, visitedObjects))
                {
                    yield return nested;
                }
            }
        }
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

    private IEnumerable<object> GetNestedTrackableObjects(object obj, HashSet<object> visitedObjects)
    {
        // Prevent infinite recursion on circular references
        if (!visitedObjects.Add(obj))
        {
            yield break;
        }

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
                if (!visitedObjects.Add(value))
                {
                    continue;
                }

                yield return value;

                if (!PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    continue;
                }

                foreach (var nested in GetNestedTrackableObjects(value, visitedObjects))
                {
                    yield return nested;
                }
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
                if (!visitedObjects.Add(value))
                {
                    continue;
                }

                yield return value;

                if (!PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    continue;
                }

                foreach (var nested in GetNestedTrackableObjects(value, visitedObjects))
                {
                    yield return nested;
                }
            }
        }
    }
}
