namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    public abstract IEnumerable<T> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T1, T2> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    public abstract IEnumerable<(T1, T2)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T1, T2, T3> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    public abstract IEnumerable<(T1, T2, T3)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T1, T2, T3, T4> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    public abstract IEnumerable<(T1, T2, T3, T4)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute<T1, T2, T3, T4, T5> : TestDataAttribute, IDataSourceGeneratorAttribute
{
    public abstract IEnumerable<(T1, T2, T3, T4, T5)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);
}