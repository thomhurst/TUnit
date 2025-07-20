using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TUnit.Core;

/// <summary>
/// Internal data source attribute for static compile-time known values
/// Used to replace StaticTestDataSource
/// </summary>
internal sealed class StaticDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private readonly object?[][] _data;
    
    public StaticDataSourceAttribute(params object?[][] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var row in _data)
        {
            var clonedRow = CloneArguments(row);
            yield return () => Task.FromResult<object?[]?>(clonedRow);
        }
        
        await Task.CompletedTask; // Suppress CS1998
    }
    
    private static object?[] CloneArguments(object?[] args)
    {
        var cloned = new object?[args.Length];
        Array.Copy(args, cloned, args.Length);
        return cloned;
    }
}