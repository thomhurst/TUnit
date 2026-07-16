namespace TUnit.Core;

/// <summary>
/// Marker interface for metadata types (Property, Parameter, Class, Method).
/// Does not define Type property to allow each implementation to have its own AOT annotations.
/// </summary>
public interface IMemberMetadata
{
    string Name { get; }
}
