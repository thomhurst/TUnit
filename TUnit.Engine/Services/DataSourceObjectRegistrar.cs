using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Services;

/// <summary>
/// Handles registration of objects created by data sources with the ObjectLifetimeManager.
/// </summary>
internal class DataSourceObjectRegistrar(ObjectLifetimeManager objectLifetimeManager)
{
    /// <summary>
    /// Registers the test instance and recursively finds and registers all objects
    /// created by data source attributes.
    /// </summary>
    public async Task RegisterTestInstanceAndDataSourceObjectsAsync(object testInstance, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Register the test instance itself
        objectLifetimeManager.RegisterObject(testInstance);

        // Initialize and register nested data source objects
        await DataSourceInitializer.InitializeAsync(
            testInstance,
            dataGeneratorMetadata.TestInformation
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds and registers all objects that have already been created by data sources
    /// on the given instance.
    /// </summary>
    public void RegisterExistingDataSourceObjects(object instance)
    {
        var visited = new HashSet<object>();
        RegisterExistingDataSourceObjectsRecursive(instance, visited);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Type's properties might be trimmed", Justification = "Test objects are preserved by the framework")]
    private void RegisterExistingDataSourceObjectsRecursive(object? obj, HashSet<object> visited)
    {
        if (obj is null || !visited.Add(obj))
        {
            return;
        }

        // Register the object
        objectLifetimeManager.RegisterObject(obj);

        // Find properties with data attributes
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        foreach (var propertyInfo in properties)
        {
            // Check if property has a data attribute
            var hasDataAttribute = propertyInfo.GetCustomAttributesSafe().OfType<IDataAttribute>().Any();
            if (!hasDataAttribute)
            {
                continue;
            }

            // Get the property value
            var propertyValue = propertyInfo.GetValue(obj);
            if (propertyValue is not null)
            {
                // Recursively register
                RegisterExistingDataSourceObjectsRecursive(propertyValue, visited);
            }
        }
    }
}
