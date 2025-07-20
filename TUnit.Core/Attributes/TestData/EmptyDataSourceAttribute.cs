using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TUnit.Core;

/// <summary>
/// Internal data source attribute for tests with no data
/// Used to replace EmptyTestDataSource
/// </summary>
internal sealed class EmptyDataSourceAttribute : Attribute, IDataSourceAttribute
{
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult<object?[]?>(Array.Empty<object?>());
        await Task.CompletedTask; // Suppress CS1998
    }
}