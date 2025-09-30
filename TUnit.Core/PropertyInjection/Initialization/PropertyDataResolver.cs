using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core.DataSources;
using TUnit.Core.Initialization;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core.PropertyInjection.Initialization;

/// <summary>
/// Handles all data source resolution logic for property initialization.
/// Follows Single Responsibility Principle by focusing only on data resolution.
/// </summary>
internal static class PropertyDataResolver
{
    /// <summary>
    /// Resolves data from a property's data source.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Property types handled dynamically")]
    public static async Task<object?> ResolvePropertyDataAsync(PropertyInitializationContext context, DataSourceInitializer dataSourceInitializer, TestObjectInitializer testObjectInitializer)
    {
        var dataSource = await GetInitializedDataSourceAsync(context, dataSourceInitializer);
        if (dataSource == null)
        {
            return null;
        }

        var dataGeneratorMetadata = CreateDataGeneratorMetadata(context, dataSource);
        var dataRows = dataSource.GetDataRowsAsync(dataGeneratorMetadata);

        // Get the first value from the data source
        await foreach (var factory in dataRows)
        {
            var args = await factory();
            var value = ResolveValueFromArgs(context.PropertyType, args);
            
            // Resolve any Func<T> wrappers
            value = await ResolveDelegateValue(value);
            
            // Initialize the resolved value if needed
            if (value != null)
            {
                // If the resolved value is itself a data source, ensure it's initialized
                if (value is IDataSourceAttribute dataSourceValue)
                {
                    value = await dataSourceInitializer.EnsureInitializedAsync(
                        dataSourceValue,
                        context.ObjectBag,
                        context.MethodMetadata,
                        context.Events);
                }
                // Otherwise, initialize if it has injectable properties or implements IAsyncInitializer
                else if (PropertyInjectionCache.HasInjectableProperties(value.GetType()) || 
                         value is IAsyncInitializer)
                {
                    // Use TestObjectInitializer for complete initialization
                    value = await testObjectInitializer.InitializeAsync(value, context.TestContext);
                }
                
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets an initialized data source from the context.
    /// Ensures the data source is fully initialized (including property injection) before returning it.
    /// </summary>
    private static async Task<IDataSourceAttribute?> GetInitializedDataSourceAsync(PropertyInitializationContext context, DataSourceInitializer dataSourceInitializer)
    {
        IDataSourceAttribute? dataSource = null;
        
        if (context.DataSource != null)
        {
            dataSource = context.DataSource;
        }
        else if (context.SourceGeneratedMetadata != null)
        {
            // Create a new data source instance
            dataSource = context.SourceGeneratedMetadata.CreateDataSource();
        }

        if (dataSource == null)
        {
            return null;
        }

        // Ensure the data source is fully initialized before use
        // This handles property injection and IAsyncInitializer
        return await dataSourceInitializer.EnsureInitializedAsync(
            dataSource,
            context.ObjectBag,
            context.MethodMetadata,
            context.Events);
    }

    /// <summary>
    /// Creates data generator metadata for the property.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Property injection metadata")]
    private static DataGeneratorMetadata CreateDataGeneratorMetadata(
        PropertyInitializationContext context,
        IDataSourceAttribute dataSource)
    {
        if (context.SourceGeneratedMetadata != null)
        {
            // Source-generated mode
            if (context.SourceGeneratedMetadata.ContainingType == null)
            {
                throw new InvalidOperationException(
                    $"ContainingType is null for property '{context.PropertyName}'. " +
                    $"This may indicate an issue with source generator for type '{context.PropertyType.Name}'.");
            }

            var propertyMetadata = new PropertyMetadata
            {
                IsStatic = false,
                Name = context.PropertyName,
                ClassMetadata = ClassMetadataHelper.GetOrCreateClassMetadata(context.SourceGeneratedMetadata.ContainingType),
                Type = context.PropertyType,
                ReflectionInfo = PropertyHelper.GetPropertyInfo(context.SourceGeneratedMetadata.ContainingType, context.PropertyName),
                Getter = parent => PropertyHelper.GetPropertyInfo(context.SourceGeneratedMetadata.ContainingType, context.PropertyName).GetValue(parent!)!,
                ContainingTypeMetadata = ClassMetadataHelper.GetOrCreateClassMetadata(context.SourceGeneratedMetadata.ContainingType)
            };

            return DataGeneratorMetadataCreator.CreateForPropertyInjection(
                propertyMetadata,
                context.MethodMetadata,
                dataSource,
                context.TestContext,
                context.TestContext?.TestDetails.ClassInstance,
                context.Events,
                context.ObjectBag);
        }
        else if (context.PropertyInfo != null)
        {
            // Reflection mode
            return DataGeneratorMetadataCreator.CreateForPropertyInjection(
                context.PropertyInfo,
                context.PropertyInfo.DeclaringType!,
                context.MethodMetadata,
                dataSource,
                context.TestContext,
                context.Instance,
                context.Events,
                context.ObjectBag);
        }

        throw new InvalidOperationException("Cannot create data generator metadata: no property information available");
    }

    /// <summary>
    /// Resolves value from data source arguments, handling tuples.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Tuple types are created dynamically")]
    private static object? ResolveValueFromArgs(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] 
        Type propertyType, 
        object?[]? args)
    {
        return TupleValueResolver.ResolveTupleValue(propertyType, args);
    }

    /// <summary>
    /// Resolves delegate values by invoking them.
    /// </summary>
    private static async ValueTask<object?> ResolveDelegateValue(object? value)
    {
        return await PropertyValueProcessor.ResolveTestDataValueAsync(typeof(object), value);
    }
}