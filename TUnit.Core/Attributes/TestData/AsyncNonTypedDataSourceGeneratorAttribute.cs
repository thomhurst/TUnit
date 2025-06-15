namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class AsyncNonTypedDataSourceGeneratorAttribute : TestDataAttribute, IAsyncNonTypedDataSourceGeneratorAttribute
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