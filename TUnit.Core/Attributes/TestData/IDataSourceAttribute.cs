namespace TUnit.Core;

public interface IDataSourceAttribute
{
    public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata);

    /// <summary>
    /// When true, if the data source returns no data, the test will be skipped instead of failing.
    /// </summary>
    bool SkipIfEmpty { get; set; }
}
