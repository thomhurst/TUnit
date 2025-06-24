using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Interface for resolving data sources
/// </summary>
public interface IDataSourceResolver
{
    Task<IEnumerable<object?[]>> ResolveDataSource(TestDataSource dataSource);
    Task<IEnumerable<object?[]>> ResolveDataAsync(TestDataSource dataSource);
    Task<object?> ResolvePropertyDataAsync(PropertyDataSource propertyDataSource);
}