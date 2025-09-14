using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Extensions;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] T> : TypedDataSourceAttribute<T>
{
    protected abstract IAsyncEnumerable<Func<Task<T>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public override async IAsyncEnumerable<Func<Task<T>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Inject properties into the data source attribute itself if we have context
        // This is needed for custom data sources that have their own data source properties
        if (dataGeneratorMetadata is { TestInformation: not null })
        {
            await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this,
                dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag,
                dataGeneratorMetadata.TestInformation,
                dataGeneratorMetadata.TestBuilderContext.Current.Events);
        }

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    T2> : TypedDataSourceAttribute<(T1, T2)>
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public override async IAsyncEnumerable<Func<Task<(T1, T2)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Inject properties into the data source attribute itself if we have context
        if (dataGeneratorMetadata is { TestInformation: not null })
        {
            await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this,
                dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag,
                dataGeneratorMetadata.TestInformation,
                dataGeneratorMetadata.TestBuilderContext.Current.Events);
        }

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
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
    T3> : TypedDataSourceAttribute<(T1, T2, T3)>
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public override async IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Inject properties into the data source attribute itself if we have context
        if (dataGeneratorMetadata is { TestInformation: not null })
        {
            await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this,
                dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag,
                dataGeneratorMetadata.TestInformation,
                dataGeneratorMetadata.TestBuilderContext.Current.Events);
        }

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
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
    T4> : TypedDataSourceAttribute<(T1, T2, T3, T4)>
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public override async IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Inject properties into the data source attribute itself if we have context
        if (dataGeneratorMetadata is { TestInformation: not null })
        {
            await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this,
                dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag,
                dataGeneratorMetadata.TestInformation,
                dataGeneratorMetadata.TestBuilderContext.Current.Events);
        }

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
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
    T5> : TypedDataSourceAttribute<(T1, T2, T3, T4, T5)>
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public override async IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Inject properties into the data source attribute itself if we have context
        if (dataGeneratorMetadata is { TestInformation: not null })
        {
            await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this,
                dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag,
                dataGeneratorMetadata.TestInformation,
                dataGeneratorMetadata.TestBuilderContext.Current.Events);
        }

        await ObjectInitializer.InitializeAsync(this);

        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }
}
