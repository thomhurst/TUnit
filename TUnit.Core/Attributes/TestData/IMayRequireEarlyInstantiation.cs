namespace TUnit.Core;

/// <summary>
/// Marker interface for data source attributes that may conditionally require early instance creation
/// depending on their parameter attributes or other runtime conditions.
/// This allows the test builder to determine when to attempt early instantiation for generic type resolution
/// or instance method access.
/// </summary>
public interface IMayRequireEarlyInstantiation;