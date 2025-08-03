using TUnit.Core.Helpers;

namespace TUnit.Core;

public abstract class TypedDataSourceAttribute<T> : Attribute, ITypedDataSourceAttribute<T>
{
    public abstract IAsyncEnumerable<Func<Task<T>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata);
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // This method provides compatibility with the IDataSourceAttribute interface.
        // The source generator detects typed data sources and generates optimized code
        // that avoids boxing when possible by using TypedTestArguments.
        await foreach (var row in GetTypedDataRowsAsync(dataGeneratorMetadata))
        {
            var result = await row();
            yield return () => Task.FromResult<object?[]?>(result.ToObjectArray());
        }
    }
}
