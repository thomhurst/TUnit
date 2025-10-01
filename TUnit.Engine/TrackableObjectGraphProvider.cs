using TUnit.Core;
using TUnit.Core.PropertyInjection;

namespace TUnit.Engine;

internal class TrackableObjectGraphProvider
{
    public IEnumerable<object> GetTrackableObjects(TestContext testContext)
    {
        var testDetails = testContext.TestDetails;

        foreach (var classArgument in testDetails.TestClassArguments)
        {
            if (classArgument != null)
            {
                yield return classArgument;

                foreach (var nested in GetNestedTrackableObjects(classArgument))
                {
                    yield return nested;
                }
            }
        }

        foreach (var methodArgument in testDetails.TestMethodArguments)
        {
            if (methodArgument != null)
            {
                yield return methodArgument;

                foreach (var nested in GetNestedTrackableObjects(methodArgument))
                {
                    yield return nested;
                }
            }
        }

        foreach (var property in testDetails.TestClassInjectedPropertyArguments.Values)
        {
            if (property != null)
            {
                yield return property;

                foreach (var nested in GetNestedTrackableObjects(property))
                {
                    yield return nested;
                }
            }
        }
    }

    private IEnumerable<object> GetNestedTrackableObjects(object obj)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        if(!SourceRegistrar.IsEnabled)
        {
            foreach (var prop in plan.ReflectionProperties)
            {
                var value = prop.Property.GetValue(obj);
                yield return value;

                if (value != null && PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    foreach (var nested in GetNestedTrackableObjects(value))
                    {
                        yield return nested;
                    }
                }
            }
        }
        else
        {
            foreach (var prop in plan.SourceGeneratedProperties)
            {
                var value = prop.GetValue(obj);
                yield return value;

                if (value != null && PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    foreach (var nested in GetNestedTrackableObjects(value))
                    {
                        yield return nested;
                    }
                }
            }
        }
    }
}
