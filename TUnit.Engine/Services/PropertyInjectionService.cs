using TUnit.Core;
using TUnit.Core.Tracking;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Data;

namespace TUnit.Engine.Services;

internal sealed class PropertyInjectionService
{
    private static readonly GetOnlyDictionary<object, Task> _injectionTasks = new();

    /// <summary>
    /// Injects properties with data sources into argument objects just before test execution.
    /// This ensures properties are only initialized when the test is about to run.
    /// </summary>
    public static async Task InjectPropertiesIntoArgumentsAsync(object?[] arguments, TestContext testContext)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        foreach (var argument in arguments)
        {
            if (argument != null && ShouldInjectProperties(argument))
            {
                await InjectPropertiesIntoObjectAsync(argument, testContext);
            }
        }
    }

    /// <summary>
    /// Determines if an object should have properties injected based on its type and whether it has nested data sources.
    /// </summary>
    private static bool ShouldInjectProperties(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        var type = obj.GetType();

        if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType)
        {
            return false;
        }

        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        if (type.Assembly == typeof(object).Assembly)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Recursively injects properties with data sources into a single object using the new static property source system.
    /// The PropertySource includes inherited properties, so we only need to check the concrete type.
    /// After injection, handles tracking, initialization, and recursive injection.
    /// </summary>
    private static async Task InjectPropertiesIntoObjectAsync(object instance, TestContext testContext)
    {
        try
        {
            var type = instance.GetType();

            await _injectionTasks.GetOrAdd(instance, async _ =>
            {
                var propertySource = PropertySourceRegistry.GetSource(type);

                if (propertySource?.ShouldInitialize == true)
                {
                    // First, create all data source objects
                    var propertyValues = await propertySource.InitializeAsync(instance);

                    // Then handle each created value: track, initialize, set property, and setup cleanup
                    foreach (var kvp in propertyValues)
                    {
                        await ProcessInjectedPropertyValue(instance, kvp.Key, kvp.Value, testContext);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to inject properties for type '{instance?.GetType().Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Processes a single injected property value: tracks it, initializes it, sets it on the instance, and handles cleanup.
    /// </summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075", Justification = "Property reflection is expected in testing framework")]
    private static async Task ProcessInjectedPropertyValue(object instance, string propertyName, object? propertyValue, TestContext testContext)
    {
        if (propertyValue == null)
        {
            return;
        }

        var trackedValue = ObjectTrackerProvider.Track(propertyValue);

        if (trackedValue != null && ShouldInjectProperties(trackedValue))
        {
            await InjectPropertiesIntoObjectAsync(trackedValue, testContext);
        }

        await ObjectInitializer.InitializeAsync(trackedValue);

        var type = instance.GetType();
        var property = type.GetProperty(propertyName);

        if (property == null || !property.CanWrite)
        {
            return;
        }

        property.SetValue(instance, trackedValue);

        testContext.Events.OnDispose += async (o, context) =>
        {
            await ObjectTrackerProvider.Untrack(trackedValue);
        };
    }

}
