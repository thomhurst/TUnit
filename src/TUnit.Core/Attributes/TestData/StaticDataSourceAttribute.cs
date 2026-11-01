using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Internal data source attribute for static compile-time known values
/// Used to replace StaticTestDataSource
/// </summary>
internal sealed class StaticDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private readonly object?[][] _data;

    /// <inheritdoc />
    public bool SkipIfEmpty { get; set; }

    /// <inheritdoc />
    public bool DeferEnumeration { get; set; }

    /// <summary>
    /// The data source whose rows this instance holds a subset of, when the engine splits a source's
    /// rows across generic instantiations. The test keeps that source's attribute behaviour, such as
    /// its <see cref="Interfaces.ITestRegisteredEventReceiver"/> callback.
    /// </summary>
    public IDataSourceAttribute? PartitionOf { get; init; }

    /// <summary>
    /// The values to return from the first fetch of each row instead of that row, or null at an index to always
    /// return the row. Discovery sets it to the values it evaluated from a wrapped factory row to infer a generic
    /// class's type arguments, so the first test gets those values, and later tests, such as other method rows or
    /// repeats, invoke the factory for their own objects (#7210). Each box is handed out once, including when several
    /// sources share it.
    /// </summary>
    public StrongBox<object?[]?>?[]? FirstFetchRows { get; init; }

    public StaticDataSourceAttribute(params object?[][] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        for (var i = 0; i < _data.Length; i++)
        {
            var clonedRow = CloneArguments(_data[i]);

            if (FirstFetchRows?[i] is { } firstFetch)
            {
                yield return () => Task.FromResult<object?[]?>(Interlocked.Exchange(ref firstFetch.Value, null) ?? clonedRow);
                continue;
            }

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