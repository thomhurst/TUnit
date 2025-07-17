namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class UntypedDataSourceGeneratorAttribute : AsyncUntypedDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected sealed override async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }
        await Task.CompletedTask;
    }

}
