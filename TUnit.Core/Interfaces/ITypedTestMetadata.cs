namespace TUnit.Core.Interfaces;

/// <summary>
/// Marker interface for typed test metadata
/// </summary>
public interface ITypedTestMetadata
{
    /// <summary>
    /// Gets the test class type
    /// </summary>
    Type TestClassType { get; }
}