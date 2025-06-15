using TUnit.Core.Extensions;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T> : AsyncDataSourceGeneratorAttribute<T>, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected override async IAsyncEnumerable<Func<Task<T>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }
        await Task.CompletedTask;
    }

    public IEnumerable<Func<T>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
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

    IEnumerable<Func<object?[]?>> IDataSourceGeneratorAttribute.Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var dataSourceDelegate in Generate(dataGeneratorMetadata))
        {
            yield return () => [dataSourceDelegate()];
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T1, T2> : AsyncDataSourceGeneratorAttribute<T1, T2>, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<(T1, T2)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected override async IAsyncEnumerable<Func<Task<(T1, T2)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }
        await Task.CompletedTask;
    }

    public IEnumerable<Func<(T1, T2)>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
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

    IEnumerable<Func<object?[]?>> IDataSourceGeneratorAttribute.Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var dataSourceDelegate in Generate(dataGeneratorMetadata))
        {
            yield return () => dataSourceDelegate().ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T1, T2, T3> : AsyncDataSourceGeneratorAttribute<T1, T2, T3>, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected override async IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }
        await Task.CompletedTask;
    }

    public IEnumerable<Func<(T1, T2, T3)>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
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

    IEnumerable<Func<object?[]?>> IDataSourceGeneratorAttribute.Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var dataSourceDelegate in Generate(dataGeneratorMetadata))
        {
            yield return () => dataSourceDelegate().ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T1, T2, T3, T4> : AsyncDataSourceGeneratorAttribute<T1, T2, T3, T4>, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<(T1, T2, T3, T4)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected override async IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }
        await Task.CompletedTask;
    }

    public IEnumerable<Func<(T1, T2, T3, T4)>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
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

    IEnumerable<Func<object?[]?>> IDataSourceGeneratorAttribute.Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var dataSourceDelegate in Generate(dataGeneratorMetadata))
        {
            yield return () => dataSourceDelegate().ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T1, T2, T3, T4, T5> : AsyncDataSourceGeneratorAttribute<T1, T2, T3, T4, T5>, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<(T1, T2, T3, T4, T5)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected override async IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }
        await Task.CompletedTask;
    }

    public IEnumerable<Func<(T1, T2, T3, T4, T5)>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
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

    IEnumerable<Func<object?[]?>> IDataSourceGeneratorAttribute.Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var dataSourceDelegate in Generate(dataGeneratorMetadata))
        {
            yield return () => dataSourceDelegate().ToObjectArray();
        }
    }
}
