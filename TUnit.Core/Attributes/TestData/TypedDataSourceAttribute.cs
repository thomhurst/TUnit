using TUnit.Core.Helpers;

namespace TUnit.Core;

public abstract class TypedDataSourceAttribute<T> : Attribute, ITypedDataSourceAttribute<T>
{
    public abstract IAsyncEnumerable<Func<Task<T>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata);
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Note: This method boxes value types when converting to object?[]
        // Future optimization: The source generator could detect when a data source
        // implements ITypedDataSourceAttribute<T> and generate specialized code
        // that calls GetTypedDataRowsAsync directly, avoiding boxing for value types
        await foreach (var row in GetTypedDataRowsAsync(dataGeneratorMetadata))
        {
            var result = await row();
            yield return () => Task.FromResult<object?[]?>(result.ToObjectArray());
        }
    }
}
