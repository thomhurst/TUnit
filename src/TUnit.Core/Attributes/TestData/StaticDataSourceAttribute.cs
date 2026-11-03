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

    // Shared by the raw source and every generic partition of the same row.
    public EvaluatedDataRow?[]? EvaluatedRows { get; init; }

    public StaticDataSourceAttribute(params object?[][] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        for (var i = 0; i < _data.Length; i++)
        {
            if (EvaluatedRows?[i] is { } evaluated)
            {
                yield return evaluated.FetchAsync;
            }
            else
            {
                var clonedRow = CloneArguments(_data[i]);
                yield return () => Task.FromResult<object?[]?>(clonedRow);
            }
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