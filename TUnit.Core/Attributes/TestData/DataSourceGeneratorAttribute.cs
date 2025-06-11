using TUnit.Core.Extensions;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    public IEnumerable<Func<T>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            // TODO: Async
            dataGeneratorMetadata.TestBuilderContext.Current.InitializeAsync().GetAwaiter().GetResult();
            yield return generateDataSource;
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
public abstract class DataSourceGeneratorAttribute<T1, T2> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<(T1, T2)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    public  IEnumerable<Func<(T1, T2)>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSources(dataGeneratorMetadata);
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
public abstract class DataSourceGeneratorAttribute<T1, T2, T3> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    public  IEnumerable<Func<(T1, T2, T3)>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSources(dataGeneratorMetadata);
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
public abstract class DataSourceGeneratorAttribute<T1, T2, T3, T4> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<(T1, T2, T3, T4)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    public  IEnumerable<Func<(T1, T2, T3, T4)>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSources(dataGeneratorMetadata);
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
public abstract class DataSourceGeneratorAttribute<T1, T2, T3, T4, T5> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<(T1, T2, T3, T4, T5)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    public  IEnumerable<Func<(T1, T2, T3, T4, T5)>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSources(dataGeneratorMetadata);
    }

    IEnumerable<Func<object?[]?>> IDataSourceGeneratorAttribute.Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var dataSourceDelegate in Generate(dataGeneratorMetadata))
        {
            yield return () => dataSourceDelegate().ToObjectArray();
        }
    }
}
