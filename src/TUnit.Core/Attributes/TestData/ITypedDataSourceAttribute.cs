namespace TUnit.Core;

public interface ITypedDataSourceAttribute<T> : IDataSourceAttribute
{
    public IAsyncEnumerable<Func<Task<T>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata);
}
