using TUnit.Core;

namespace TUnit.Engine;

internal class EmptyDataSourceAttribute : IDataSourceAttribute
{
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult<object?[]?>([]);
        await Task.CompletedTask;
    }
}
