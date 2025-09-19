using System.Threading.Tasks;

namespace TUnit.Core.PropertyInjection.Initialization.Strategies;

/// <summary>
/// Strategy for initializing properties using source-generated metadata.
/// Optimized for AOT and performance.
/// </summary>
internal sealed class SourceGeneratedPropertyStrategy : IPropertyInitializationStrategy
{
    /// <summary>
    /// Determines if this strategy can handle source-generated properties.
    /// </summary>
    public bool CanHandle(PropertyInitializationContext context)
    {
        return context.SourceGeneratedMetadata != null && SourceRegistrar.IsEnabled;
    }

    /// <summary>
    /// Initializes a property using source-generated metadata.
    /// </summary>
    public async Task InitializePropertyAsync(PropertyInitializationContext context)
    {
        if (context.SourceGeneratedMetadata == null)
        {
            return;
        }

        // Step 1: Resolve data from the data source
        var resolvedValue = await PropertyDataResolver.ResolvePropertyDataAsync(context);
        if (resolvedValue == null)
        {
            return;
        }

        context.ResolvedValue = resolvedValue;

        // Step 2: Track the property value
        PropertyTrackingService.TrackPropertyValue(context, resolvedValue);

        // Step 3: Handle nested property initialization
        if (PropertyInjectionCache.HasInjectableProperties(resolvedValue.GetType()))
        {
            // Mark for recursive processing
            await InitializeNestedProperties(context, resolvedValue);
        }
        else
        {
            // Just initialize the object
            await ObjectInitializer.InitializeAsync(resolvedValue);
        }

        // Step 4: Set the property value
        context.SourceGeneratedMetadata.SetProperty(context.Instance, resolvedValue);

        // Step 5: Add to test context tracking
        PropertyTrackingService.AddToTestContext(context, resolvedValue);
    }

    /// <summary>
    /// Handles initialization of nested properties.
    /// </summary>
    private async Task InitializeNestedProperties(PropertyInitializationContext context, object propertyValue)
    {
        // This will be handled by the PropertyInjectionService recursively
        // We just need to ensure it's initialized
        await PropertyInjectionService.InjectPropertiesIntoObjectAsync(
            propertyValue,
            context.ObjectBag,
            context.MethodMetadata,
            context.Events);
    }
}