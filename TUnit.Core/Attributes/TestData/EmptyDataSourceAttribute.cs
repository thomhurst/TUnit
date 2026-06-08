namespace TUnit.Core;

/// <summary>
/// Internal data source attribute for tests with no data
/// Used to replace EmptyTestDataSource
/// </summary>
internal sealed class EmptyDataSourceAttribute : Attribute, IDataSourceAttribute
{
    /// <inheritdoc />
    public bool SkipIfEmpty { get; set; }

    /// <inheritdoc />
    // Always a single (empty) row, so deferring its enumeration would be pure overhead.
    public bool DeferEnumeration { get => false; set { } }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult<object?[]?>([
        ]);
        await Task.CompletedTask; // Suppress CS1998
    }
}