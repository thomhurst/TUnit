using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T> : AsyncDataSourceGeneratorAttribute<T>
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
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T2> : AsyncDataSourceGeneratorAttribute<T1, T2>
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
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T3> : AsyncDataSourceGeneratorAttribute<T1, T2, T3>
{
    protected abstract IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected sealed override async IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }

        await Task.CompletedTask;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T4> : AsyncDataSourceGeneratorAttribute<T1, T2, T3, T4>
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
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T4,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T5> : AsyncDataSourceGeneratorAttribute<T1, T2, T3, T4, T5>
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
}
