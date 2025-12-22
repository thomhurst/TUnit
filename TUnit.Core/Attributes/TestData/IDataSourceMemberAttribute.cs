namespace TUnit.Core;

/// <summary>
/// Marker interface for attributes that provide data values for individual parameters
/// within a data source context (e.g., matrix testing).
/// Attributes implementing this interface will be cached by the source generator
/// for AOT-compatible runtime access.
/// </summary>
public interface IDataSourceMemberAttribute;
