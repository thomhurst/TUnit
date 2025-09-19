using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core.Services;
using TUnit.Core.Tracking;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Processes property values during injection.
/// Handles value resolution, tracking, and recursive injection.
/// </summary>
internal sealed class PropertyValueProcessor
{
    public PropertyValueProcessor()
    {
    }

    /// <summary>
    /// Processes a single injected property value: tracks it, initializes it, sets it on the instance.
    /// </summary>
    public async Task ProcessInjectedValueAsync(
        object instance,
        object? propertyValue,
        Action<object, object?> setProperty,
        Dictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (propertyValue == null)
        {
            return;
        }

        // Track the object for disposal
        ObjectTracker.TrackObject(events, propertyValue);
        ObjectTracker.TrackOwnership(instance, propertyValue);

        // Check if the property value itself needs property injection
        if (ShouldInjectProperties(propertyValue))
        {
            // Recursively inject properties into the property value
            await PropertyInjectionService.InjectPropertiesIntoObjectAsync(
                propertyValue, objectBag, methodMetadata, events);
        }
        else
        {
            // Just initialize the object
            await ObjectInitializer.InitializeAsync(propertyValue);
        }

        // Set the property value on the instance
        setProperty(instance, propertyValue);
    }

    /// <summary>
    /// Resolves Func<T> values by invoking them without using reflection (AOT-safe).
    /// </summary>
    public static ValueTask<object?> ResolveTestDataValueAsync(Type type, object? value)
    {
        if (value == null)
        {
            return new ValueTask<object?>(result: null);
        }

        if (value is Delegate del)
        {
            // Use DynamicInvoke which is AOT-safe for parameterless delegates
            var result = del.DynamicInvoke();
            return new ValueTask<object?>(result);
        }

        return new ValueTask<object?>(value);
    }

    /// <summary>
    /// Determines if an object should have properties injected based on whether it has properties with data source attributes.
    /// </summary>
    private static bool ShouldInjectProperties(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        var type = obj.GetType();
        
        // Check if this type has any injectable properties
        // This will use cached results from PropertyInjectionService
        return PropertyInjectionCache.HasInjectableProperties(type);
    }
}