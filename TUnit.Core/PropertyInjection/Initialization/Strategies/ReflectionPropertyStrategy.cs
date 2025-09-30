using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using TUnit.Core.DataSources;
using TUnit.Core.Initialization;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.Core.PropertyInjection.Initialization.Strategies;

/// <summary>
/// Strategy for initializing properties using reflection.
/// Used when source generation is not available.
/// </summary>
internal sealed class ReflectionPropertyStrategy : IPropertyInitializationStrategy
{
    private readonly DataSourceInitializer _dataSourceInitializer;
    private readonly ObjectRegistrationService _objectRegistrationService;

    public ReflectionPropertyStrategy(DataSourceInitializer dataSourceInitializer, ObjectRegistrationService objectRegistrationService)
    {
        _dataSourceInitializer = dataSourceInitializer ?? throw new System.ArgumentNullException(nameof(dataSourceInitializer));
        _objectRegistrationService = objectRegistrationService ?? throw new System.ArgumentNullException(nameof(objectRegistrationService));
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

        object? resolvedValue = null;

        // Check if property was pre-resolved during registration
        if (context.TestContext?.TestDetails.TestClassInjectedPropertyArguments.TryGetValue(context.PropertyName, out resolvedValue) == true)
        {
            // Use pre-resolved value - it was already tracked during registration
            context.ResolvedValue = resolvedValue;

            // Initialize if needed (was skipped during registration)
            if (resolvedValue is IAsyncInitializer asyncInitializer)
            {
                await asyncInitializer.InitializeAsync();
            }

            // Set up ownership relationship
            if (resolvedValue != null)
            {
                ObjectTracker.TrackOwnership(context.Instance, resolvedValue);
            }
        }
        else
        {
            // Step 1: Resolve data from the data source (execution-time resolution)
            resolvedValue = await PropertyDataResolver.ResolvePropertyDataAsync(context, _dataSourceInitializer, _objectRegistrationService);
            if (resolvedValue == null)
            {
                return;
            }

            context.ResolvedValue = resolvedValue;

            // Step 2: Track the property value (only for non-pre-resolved properties)
            PropertyLifecycleTracker.TrackPropertyValue(context, resolvedValue);
        }

        // Step 3: Set the property value
        // The value has already been initialized by PropertyDataResolver if needed
        context.PropertySetter(context.Instance, resolvedValue);

        // Step 4: Add to test context tracking (if not already there)
        if (context.TestContext != null && !context.TestContext.TestDetails.TestClassInjectedPropertyArguments.ContainsKey(context.PropertyName))
        {
            PropertyLifecycleTracker.AddToTestContext(context, resolvedValue);
        }
    }

}