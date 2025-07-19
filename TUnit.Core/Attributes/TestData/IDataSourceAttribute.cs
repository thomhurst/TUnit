namespace TUnit.Core;

public interface IDataSourceAttribute
{
    public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata);
}
