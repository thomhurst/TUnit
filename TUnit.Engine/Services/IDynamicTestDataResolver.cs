using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Interface for resolving dynamic test data sources
/// </summary>
public interface IDynamicTestDataResolver
{
    /// <summary>
    /// Resolves a dynamic test data source into concrete test data
    /// </summary>
    /// <param name="dynamicSource">The dynamic data source to resolve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test data arrays</returns>
    Task<IEnumerable<object?[]>> ResolveDynamicDataSourceAsync(
        DynamicTestDataSource dynamicSource,
        CancellationToken cancellationToken = default);
}