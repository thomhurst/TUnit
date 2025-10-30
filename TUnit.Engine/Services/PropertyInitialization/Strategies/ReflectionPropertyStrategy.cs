using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.PropertyInjection.Initialization;

namespace TUnit.Engine.Services.PropertyInitialization;

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
        _dataSourceInitializer = dataSourceInitializer ?? throw new ArgumentNullException(nameof(dataSourceInitializer));
        _objectRegistrationService = objectRegistrationService ?? throw new ArgumentNullException(nameof(objectRegistrationService));
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
    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode throws at runtime if used in AOT, property data resolver handles reflection appropriately")]
    #endif
    public async Task InitializePropertyAsync(PropertyInitializationContext context)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new Exception("Using TUnit Reflection mechanisms isn't supported in AOT mode");
        }
#endif

        if (context.PropertyInfo == null || context.DataSource == null)
        {
            return;
        };

        object? resolvedValue = null;

        // Check if property was pre-resolved during registration
        if (context.TestContext?.Metadata.TestDetails.TestClassInjectedPropertyArguments.TryGetValue(context.PropertyName, out resolvedValue) == true)
        {
            // Use pre-resolved value - it was already initialized during first resolution
            context.ResolvedValue = resolvedValue;
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
        }

        // Step 3: Set the property value
        // The value has already been initialized by PropertyDataResolver if needed
        context.PropertySetter(context.Instance, resolvedValue);

        // Step 4: Add to test context tracking (if not already there)
        if (context.TestContext != null && !context.TestContext.Metadata.TestDetails.TestClassInjectedPropertyArguments.ContainsKey(context.PropertyName))
        {
            context.TestContext.Metadata.TestDetails.TestClassInjectedPropertyArguments[context.PropertyName] = resolvedValue;
        }
    }

}
