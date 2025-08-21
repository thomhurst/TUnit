namespace TUnit.Core;

/// <summary>
/// Interface for data sources that support shared instances across different scopes.
/// </summary>
public interface ISharedDataSource
{
    /// <summary>
    /// Gets the shared types supported by this data source.
    /// </summary>
    IEnumerable<SharedType> GetSharedTypes();
    
    /// <summary>
    /// Gets the keys used for keyed shared instances.
    /// </summary>
    IEnumerable<string> GetKeys();
}