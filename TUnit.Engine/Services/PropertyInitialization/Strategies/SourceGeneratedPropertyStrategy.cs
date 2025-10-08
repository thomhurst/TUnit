using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.PropertyInjection.Initialization;

namespace TUnit.Engine.Services.PropertyInitialization;

/// <summary>
/// Strategy for initializing properties using source-generated metadata.
/// Optimized for AOT and performance.
/// </summary>
internal sealed class SourceGeneratedPropertyStrategy : IPropertyInitializationStrategy
{
    private readonly DataSourceInitializer _dataSourceInitializer;
    private readonly ObjectRegistrationService _objectRegistrationService;

    public SourceGeneratedPropertyStrategy(DataSourceInitializer dataSourceInitializer, ObjectRegistrationService objectRegistrationService)
    {
        _dataSourceInitializer = dataSourceInitializer ?? throw new System.ArgumentNullException(nameof(dataSourceInitializer));
        _objectRegistrationService = objectRegistrationService ?? throw new System.ArgumentNullException(nameof(objectRegistrationService));
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
    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property data resolver handles both AOT and reflection modes appropriately")]
    #endif
    public async Task InitializePropertyAsync(PropertyInitializationContext context)
    {
        if (context.SourceGeneratedMetadata == null)
        {
            return;
        }

        object? resolvedValue = null;

        // Check if property was pre-resolved during registration
        if (context.TestContext?.TestDetails.TestClassInjectedPropertyArguments.TryGetValue(context.PropertyName, out resolvedValue) == true)
        {
            // Use pre-resolved value - it was already initialized during first resolution
            context.ResolvedValue = resolvedValue;
        }
        else
        {
            resolvedValue = await PropertyDataResolver.ResolvePropertyDataAsync(context, _dataSourceInitializer, _objectRegistrationService);
            if (resolvedValue == null)
            {
                return;
            }

            context.ResolvedValue = resolvedValue;
        }

        context.SourceGeneratedMetadata.SetProperty(context.Instance, resolvedValue);

        if (context.TestContext != null && !context.TestContext.TestDetails.TestClassInjectedPropertyArguments.ContainsKey(context.PropertyName))
        {
            context.TestContext.TestDetails.TestClassInjectedPropertyArguments[context.PropertyName] = resolvedValue;
        }
    }

}
