namespace TUnit.Core;

/// <summary>
/// Provides test data from an async data source generator attribute.
/// </summary>
public class AsyncDataGeneratorProvider : IDataProvider
{
    private readonly IDataSourceAttribute _generator;
    private readonly DataGeneratorMetadata _metadata;

    /// <param name="generator">The async data source generator attribute.</param>
    /// <param name="metadata">The metadata for data generation.</param>
    public AsyncDataGeneratorProvider(IDataSourceAttribute generator, DataGeneratorMetadata metadata)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<object?[]>> GetData()
    {
        var results = new List<object?[]>();

        await foreach (var dataFactory in _generator.GetDataRowsAsync(_metadata))
        {
            var data = await dataFactory();
            if (data != null)
            {
                results.Add(data);
            }
        }

        return results;
    }
}
