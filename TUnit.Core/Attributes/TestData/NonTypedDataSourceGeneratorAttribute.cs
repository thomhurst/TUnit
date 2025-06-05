namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class NonTypedDataSourceGeneratorAttribute : TestDataAttribute, INonTypedDataSourceGeneratorAttribute, IDataSourceGeneratorAttribute
{
    public abstract IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);
    IEnumerable<Func<object?[]?>> IDataSourceGeneratorAttribute.GenerateDataSourcesInternal(DataGeneratorMetadata dataGeneratorMetadata) => GenerateDataSources(dataGeneratorMetadata);
}
