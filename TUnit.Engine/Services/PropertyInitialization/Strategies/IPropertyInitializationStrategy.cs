using TUnit.Core.PropertyInjection.Initialization;

namespace TUnit.Engine.Services.PropertyInitialization;

/// <summary>
/// Defines the contract for property initialization strategies.
/// Follows Strategy pattern for flexible initialization approaches.
/// </summary>
internal interface IPropertyInitializationStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the given context.
    /// </summary>
    bool CanHandle(PropertyInitializationContext context);

    /// <summary>
    /// Initializes a property based on the provided context.
    /// </summary>
    Task InitializePropertyAsync(PropertyInitializationContext context);
}