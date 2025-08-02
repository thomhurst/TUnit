using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Extensions;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] T> : TestDataAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<T>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public async IAsyncEnumerable<Func<Task<T>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this, dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag, dataGeneratorMetadata.TestInformation, dataGeneratorMetadata.TestBuilderContext.Current.Events);

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }


    public override async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => [await dataSourceDelegate()];
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T2> : TestDataAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public async IAsyncEnumerable<Func<Task<(T1, T2)>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this, dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag, dataGeneratorMetadata.TestInformation, dataGeneratorMetadata.TestBuilderContext.Current.Events);

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }


    public override async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => (await dataSourceDelegate()).ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T3> : TestDataAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public async IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this, dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag, dataGeneratorMetadata.TestInformation, dataGeneratorMetadata.TestBuilderContext.Current.Events);

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }


    public override async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => (await dataSourceDelegate()).ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T4> : TestDataAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public async IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this, dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag, dataGeneratorMetadata.TestInformation, dataGeneratorMetadata.TestBuilderContext.Current.Events);

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }


    public override async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => (await dataSourceDelegate()).ToObjectArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T4,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T5> : TestDataAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public async IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this, dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag, dataGeneratorMetadata.TestInformation, dataGeneratorMetadata.TestBuilderContext.Current.Events);

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }


    public override async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataSourceDelegate in GenerateAsync(dataGeneratorMetadata))
        {
            yield return async () => (await dataSourceDelegate()).ToObjectArray();
        }
    }
}
