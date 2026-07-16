using TUnit.Core;

namespace TUnit.Engine;

internal class EmptyDataSourceAttribute : IDataSourceAttribute
{
    /// <inheritdoc />
    public bool SkipIfEmpty { get; set; }

    /// <inheritdoc />
    // Always a single (empty) row, so deferring its enumeration would be pure overhead.
    public bool DeferEnumeration { get => false; set { } }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult<object?[]?>([]);
        await Task.CompletedTask;
    }
}
