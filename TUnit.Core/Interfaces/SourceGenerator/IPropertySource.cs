namespace TUnit.Core.Interfaces.SourceGenerator;

/// <summary>
/// Represents a source that provides property injection.
/// </summary>
public interface IPropertySource
{
    /// <summary>
    /// Gets the type that contains the property.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Gets a value indicating whether the property should be initialized.
    /// </summary>
    bool ShouldInitialize { get; }

    /// <summary>
    /// Initializes the property on the given instance and returns a dictionary of property names to their initialized values.
    /// This allows the engine to handle tracking and lifecycle management of the returned values.
    /// </summary>
    Task<Dictionary<string, object?>> InitializeAsync(object instance);
}
