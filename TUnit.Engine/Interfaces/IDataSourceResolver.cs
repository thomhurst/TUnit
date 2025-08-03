using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for resolving data sources
/// </summary>
public interface IDataSourceResolver
{
    Task<IEnumerable<object?[]>> ResolveDataSource(IDataSourceAttribute dataSource);
    Task<IEnumerable<object?[]>> ResolveDataAsync(IDataSourceAttribute dataSource);
    Task<object?> ResolvePropertyDataAsync(PropertyDataSource propertyDataSource);
}
