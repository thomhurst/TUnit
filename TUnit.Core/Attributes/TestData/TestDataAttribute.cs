namespace TUnit.Core;

public abstract class TestDataAttribute : TUnitAttribute, IDataSourceAttribute
{
    public abstract IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata);
}
