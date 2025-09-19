using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using TUnit.Core.DataSources;
using TUnit.Core.Initialization;

namespace TUnit.Core.PropertyInjection.Initialization.Strategies;

/// <summary>
/// Strategy for initializing properties using reflection.
/// Used when source generation is not available.
/// </summary>
internal sealed class ReflectionPropertyStrategy : IPropertyInitializationStrategy
{
    private readonly DataSourceInitializer _dataSourceInitializer;
    private readonly TestObjectInitializer _testObjectInitializer;

    public ReflectionPropertyStrategy(DataSourceInitializer dataSourceInitializer, TestObjectInitializer testObjectInitializer)
    {
        _dataSourceInitializer = dataSourceInitializer ?? throw new System.ArgumentNullException(nameof(dataSourceInitializer));
        _testObjectInitializer = testObjectInitializer ?? throw new System.ArgumentNullException(nameof(testObjectInitializer));
    }
    /// <summary>
    /// Determines if this strategy can handle reflection-based properties.
    /// </summary>
    public bool CanHandle(PropertyInitializationContext context)
    {
        return context.PropertyInfo != null && context.DataSource != null && !SourceRegistrar.IsEnabled;
    }

    /// <summary>
    /// Initializes a property using reflection.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Reflection mode support")]
    public async Task InitializePropertyAsync(PropertyInitializationContext context)
    {
        if (context.PropertyInfo == null || context.DataSource == null)
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
        context.PropertySetter(context.Instance, resolvedValue);

        // Step 4: Add to test context tracking
        PropertyTrackingService.AddToTestContext(context, resolvedValue);
    }

}