namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class AsyncUntypedDataSourceGeneratorAttribute : TestDataAttribute, IAsyncUntypedDataSourceGeneratorAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }
}