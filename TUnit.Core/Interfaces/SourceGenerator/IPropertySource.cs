namespace TUnit.Core.Interfaces.SourceGenerator;

/// <summary>
/// Represents a source that provides property injection metadata.
/// </summary>
public interface IPropertySource
{
    /// <summary>
    /// Gets the type that contains the property.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets a value indicating whether the type has properties that need injection.
    /// </summary>
    bool ShouldInitialize { get; }

    /// <summary>
    /// Gets the metadata for all properties that need data source injection.
    /// </summary>
    IEnumerable<PropertyInjectionMetadata> GetPropertyMetadata();
}
