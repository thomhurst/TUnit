using TUnit.Core;
using TUnit.Core.Tracking;

namespace TUnit.Engine.Services;

public sealed class PropertyInjectionService
{
    /// <summary>
    /// Injects properties with data sources into argument objects just before test execution.
    /// This ensures properties are only initialized when the test is about to run.
    /// </summary>
    public static async Task InjectPropertiesIntoArgumentsAsync(object?[] arguments)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        foreach (var argument in arguments)
        {
            if (argument != null && ShouldInjectProperties(argument))
            {
                await InjectPropertiesIntoObjectAsync(argument);
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

        // Skip primitives, strings, enums, and value types
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType)
        {
            return false;
        }

        // Skip collections and arrays
        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        // Skip system types
        if (type.Assembly == typeof(object).Assembly)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Recursively injects properties with data sources into a single object using the new static property source system.
    /// The PropertySource includes inherited properties, so we only need to check the concrete type.
    /// </summary>
    private static async Task InjectPropertiesIntoObjectAsync(object instance)
    {
        try
        {
            var type = instance.GetType();

            // Use the new static property source registry
            // The PropertySource for this type includes inherited properties
            var propertySource = PropertySourceRegistry.GetSource(type);
            if (propertySource?.ShouldInitialize == true)
            {
                await propertySource.InitializeAsync(instance);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to inject properties for type '{instance?.GetType().Name}': {ex.Message}", ex);
        }
    }
}
