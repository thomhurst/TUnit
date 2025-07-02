using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// AOT-safe service for resolving dynamic test data sources using pre-compiled factories
/// </summary>
[Obsolete("This class is obsolete along with DynamicTestDataSource. Data sources now contain their delegates directly.")]
public class DataSourceResolver : IDynamicTestDataResolver
{
    /// <summary>
    /// Resolves dynamic data sources using pre-compiled AOT factories only
    /// </summary>
    public async Task<IEnumerable<object?[]>> ResolveDynamicDataSourceAsync(
        DynamicTestDataSource dynamicSource,
        CancellationToken cancellationToken = default)
    {
        // In AOT mode, use only the pre-compiled factory
        try
        {
            var factories = dynamicSource.GetDataFactories();
            var results = new List<object?[]>();
            
            foreach (var factory in factories)
            {
                results.Add(factory());
            }
            
            return await Task.FromResult(results);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No data source factory registered"))
        {
            // AOT mode requires all data sources to be pre-compiled via source generators
            throw new NotSupportedException(
                $"No data source factory registered for '{dynamicSource.FactoryKey}'. " +
                "In AOT mode, all data sources must be pre-compiled via source generators. " +
                "Ensure your test project uses source generation and not reflection-based data sources.", ex);
        }
    }
}