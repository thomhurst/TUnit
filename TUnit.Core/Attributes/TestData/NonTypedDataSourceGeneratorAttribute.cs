namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class NonTypedDataSourceGeneratorAttribute : AsyncNonTypedDataSourceGeneratorAttribute, INonTypedDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected override async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }
        await Task.CompletedTask;
    }

    public IEnumerable<Func<object?[]?>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var asyncEnumerable = GenerateAsync(dataGeneratorMetadata);
        var enumerator = asyncEnumerable.GetAsyncEnumerator();
        try
        {
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                var asyncFunc = enumerator.Current;
                yield return () => asyncFunc().GetAwaiter().GetResult();
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
