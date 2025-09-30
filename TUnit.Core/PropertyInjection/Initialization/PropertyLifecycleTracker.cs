using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.Core.PropertyInjection.Initialization;

/// <summary>
/// Centralizes all property lifecycle tracking operations during initialization.
/// Follows Single Responsibility Principle by handling only tracking concerns.
/// Renamed from PropertyTrackingService to clarify focus on lifecycle/disposal tracking.
/// </summary>
internal static class PropertyLifecycleTracker
{
    /// <summary>
    /// Tracks a property value for disposal and ownership.
    /// </summary>
    public static void TrackPropertyValue(PropertyInitializationContext context, object? propertyValue)
    {
        if (propertyValue == null)
        {
            return;
        }

        // Track the object for disposal - pure reference counting
        ObjectTracker.TrackObject(context.Events, propertyValue);

        // Track ownership relationship (this is separate from reference counting)
        if (context.ParentInstance != null)
        {
            ObjectTracker.TrackOwnership(context.ParentInstance, propertyValue);
        }
        else
        {
            ObjectTracker.TrackOwnership(context.Instance, propertyValue);
        }
    }

    /// <summary>
    /// Handles tracking for nested properties after initialization.
    /// </summary>
    public static async Task TrackNestedPropertiesAsync(
        PropertyInitializationContext context,
        object propertyValue,
        PropertyInjectionPlan plan)
    {
        if (!plan.HasProperties)
        {
            return;
        }

        if (SourceRegistrar.IsEnabled)
        {
            await TrackSourceGeneratedNestedProperties(context, propertyValue, plan);
        }
        else
        {
            await TrackReflectionNestedProperties(context, propertyValue, plan);
        }
    }

    /// <summary>
    /// Tracks nested properties for source-generated mode.
    /// </summary>
    private static async Task TrackSourceGeneratedNestedProperties(
        PropertyInitializationContext context,
        object instance,
        PropertyInjectionPlan plan)
    {
        foreach (var metadata in plan.SourceGeneratedProperties)
        {
            var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
            if (property != null && property.CanRead)
            {
                var nestedValue = property.GetValue(instance);
                if (nestedValue != null)
                {
                    TrackNestedPropertyValue(context, instance, nestedValue);
                    await InitializeNestedIfRequired(context, nestedValue);
                }
            }
        }
    }

    /// <summary>
    /// Tracks nested properties for reflection mode.
    /// </summary>
    private static async Task TrackReflectionNestedProperties(
        PropertyInitializationContext context,
        object instance,
        PropertyInjectionPlan plan)
    {
        foreach (var (property, _) in plan.ReflectionProperties)
        {
            var nestedValue = property.GetValue(instance);
            if (nestedValue != null)
            {
                TrackNestedPropertyValue(context, instance, nestedValue);
                await InitializeNestedIfRequired(context, nestedValue);
            }
        }
    }

    /// <summary>
    /// Tracks a nested property value.
    /// </summary>
    private static void TrackNestedPropertyValue(
        PropertyInitializationContext context,
        object parentInstance,
        object nestedValue)
    {
        ObjectTracker.TrackObject(context.Events, nestedValue);
        ObjectTracker.TrackOwnership(parentInstance, nestedValue);
    }

    /// <summary>
    /// Initializes nested property if it has injectable properties.
    /// </summary>
    private static async Task InitializeNestedIfRequired(
        PropertyInitializationContext context,
        object nestedValue)
    {
        if (PropertyInjectionCache.HasInjectableProperties(nestedValue.GetType()))
        {
            // This will be handled by the nested property strategy
            // Just mark it for processing
            context.VisitedObjects.TryAdd(nestedValue, 1);
        }
        else
        {
            // No nested properties, just initialize
            await ObjectInitializer.InitializeAsync(nestedValue);
        }
    }

    /// <summary>
    /// Adds property value to test context tracking.
    /// </summary>
    public static void AddToTestContext(PropertyInitializationContext context, object? propertyValue)
    {
        if (context.TestContext != null && propertyValue != null)
        {
            context.TestContext.TestDetails.TestClassInjectedPropertyArguments[context.PropertyName] = propertyValue;
        }
    }
}