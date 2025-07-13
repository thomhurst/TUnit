using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for resolving data sources
/// </summary>
public interface IDataSourceResolver
{
    Task<IEnumerable<object?[]>> ResolveDataSource(TestDataSource dataSource);
    Task<IEnumerable<object?[]>> ResolveDataAsync(TestDataSource dataSource);
    Task<object?> ResolvePropertyDataAsync(PropertyDataSource propertyDataSource);
}
