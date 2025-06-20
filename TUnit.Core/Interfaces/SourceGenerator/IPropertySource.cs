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
    /// Initializes the property on the given instance.
    /// </summary>
    Task InitializeAsync(object instance);
}