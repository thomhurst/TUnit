using TUnit.Core.Extensions;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<T> : TestDataAttribute, IAsyncDataSourceGeneratorAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<T>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public async IAsyncEnumerable<Func<Task<T>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            await dataGeneratorMetadata.TestBuilderContext.Current.InitializeAsync();
            yield return generateDataSource;
        }
    }

    async IAsyncEnumerable<Func<Task<object?[]?>>> IAsyncDataSourceGeneratorAttribute.GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => [await dataSourceDelegate()];
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<T1, T2> : TestDataAttribute, IAsyncDataSourceGeneratorAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public IAsyncEnumerable<Func<Task<(T1, T2)>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }

    async IAsyncEnumerable<Func<Task<object?[]?>>> IAsyncDataSourceGeneratorAttribute.GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => (await dataSourceDelegate()).ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<T1, T2, T3> : TestDataAttribute, IAsyncDataSourceGeneratorAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }

    async IAsyncEnumerable<Func<Task<object?[]?>>> IAsyncDataSourceGeneratorAttribute.GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => (await dataSourceDelegate()).ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<T1, T2, T3, T4> : TestDataAttribute, IAsyncDataSourceGeneratorAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }

    async IAsyncEnumerable<Func<Task<object?[]?>>> IAsyncDataSourceGeneratorAttribute.GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => (await dataSourceDelegate()).ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<T1, T2, T3, T4, T5> : TestDataAttribute, IAsyncDataSourceGeneratorAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }

    async IAsyncEnumerable<Func<Task<object?[]?>>> IAsyncDataSourceGeneratorAttribute.GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => (await dataSourceDelegate()).ToObjectArray();
        }
    }
}