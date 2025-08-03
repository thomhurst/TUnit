using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces.SourceGenerator;

/// <summary>
/// Metadata about a property that needs data source injection, generated at compile-time.
/// </summary>
public sealed class PropertyInjectionMetadata
{
    /// <summary>
    /// Gets the name of the property that needs injection.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicProperties)]
    public required Type PropertyType { get; init; }

    /// <summary>
    /// Gets the type that contains the property (the parent class).
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public required Type ContainingType { get; init; }

    /// <summary>
    /// Gets a factory function that creates the data source attribute instance.
    /// </summary>
    public required Func<IDataSourceAttribute> CreateDataSource { get; init; }

    /// <summary>
    /// Gets a delegate that sets the property value on the target instance.
    /// Handles type casting and property setting (including init-only properties).
    /// </summary>
    public required Action<object, object?> SetProperty { get; init; }
}
