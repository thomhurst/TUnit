namespace TUnit.Core;

internal class NoDataSource : IDataSourceAttribute
{
    private static readonly Task<object?[]?> _emptyRowTask = Task.FromResult<object?[]?>([]);
    public static readonly NoDataSource Instance = new();

    /// <inheritdoc />
    public bool SkipIfEmpty { get; set; }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return static () => _emptyRowTask;
        await default(ValueTask);
    }
}
