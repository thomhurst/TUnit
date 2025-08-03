using TUnit.Core.Helpers;

namespace TUnit.Core;

public abstract class TypedDataSourceAttribute<T> : Attribute, ITypedDataSourceAttribute<T>
{
    public abstract IAsyncEnumerable<Func<Task<T>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata);
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var row in GetTypedDataRowsAsync(dataGeneratorMetadata))
        {
            var result = await row();
            yield return () => Task.FromResult<object?[]?>(result.ToObjectArray());
        }
    }
}
