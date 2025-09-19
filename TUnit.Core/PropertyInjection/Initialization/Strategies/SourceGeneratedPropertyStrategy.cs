using System.Threading.Tasks;
using TUnit.Core.DataSources;
using TUnit.Core.Initialization;

namespace TUnit.Core.PropertyInjection.Initialization.Strategies;

/// <summary>
/// Strategy for initializing properties using source-generated metadata.
/// Optimized for AOT and performance.
/// </summary>
internal sealed class SourceGeneratedPropertyStrategy : IPropertyInitializationStrategy
{
    private readonly DataSourceInitializer _dataSourceInitializer;
    private readonly TestObjectInitializer _testObjectInitializer;

    public SourceGeneratedPropertyStrategy(DataSourceInitializer dataSourceInitializer, TestObjectInitializer testObjectInitializer)
    {
        _dataSourceInitializer = dataSourceInitializer ?? throw new System.ArgumentNullException(nameof(dataSourceInitializer));
        _testObjectInitializer = testObjectInitializer ?? throw new System.ArgumentNullException(nameof(testObjectInitializer));
    }
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
        var resolvedValue = await PropertyDataResolver.ResolvePropertyDataAsync(context, _dataSourceInitializer, _testObjectInitializer);
        if (resolvedValue == null)
        {
            return;
        }

        context.ResolvedValue = resolvedValue;

        // Step 2: Track the property value
        PropertyTrackingService.TrackPropertyValue(context, resolvedValue);

        // Step 3: Set the property value
        // The value has already been initialized by PropertyDataResolver if needed
        context.SourceGeneratedMetadata.SetProperty(context.Instance, resolvedValue);

        // Step 4: Add to test context tracking
        PropertyTrackingService.AddToTestContext(context, resolvedValue);
    }

}