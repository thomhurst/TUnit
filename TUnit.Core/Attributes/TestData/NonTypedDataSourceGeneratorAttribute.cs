namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class NonTypedDataSourceGeneratorAttribute : TestDataAttribute, INonTypedDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    public IEnumerable<Func<object?[]?>> Generate(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSources(dataGeneratorMetadata);
    }
}
