namespace TUnit.Core;

/// <summary>
/// Internal data source attribute for tests with no data
/// Used to replace EmptyTestDataSource
/// </summary>
internal sealed class EmptyDataSourceAttribute : Attribute, IDataSourceAttribute
{
    /// <inheritdoc />
    public bool SkipIfEmpty { get; set; }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult<object?[]?>([
        ]);
        await Task.CompletedTask; // Suppress CS1998
    }
}