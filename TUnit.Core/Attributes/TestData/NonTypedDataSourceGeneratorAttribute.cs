using TUnit.Core.Helpers;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class UntypedDataSourceGeneratorAttribute : AsyncUntypedDataSourceGeneratorAttribute
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
        foreach (var asyncFunc in AsyncToSyncHelper.EnumerateSync(asyncEnumerable))
        {
            yield return () => AsyncToSyncHelper.RunSync(asyncFunc);
        }
    }
}
