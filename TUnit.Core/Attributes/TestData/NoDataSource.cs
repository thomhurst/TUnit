namespace TUnit.Core;

internal class NoDataSource : IDataSourceAttribute
{
    public static readonly NoDataSource Instance = new();
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult<object?[]?>([]);
        await default(ValueTask);
    }
}
