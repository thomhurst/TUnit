namespace TUnit.Core;

internal interface IDataSourceGeneratorAttribute : IDataAttribute
{
    internal IEnumerable<Func<object?[]?>> GenerateDataSourcesInternal(DataGeneratorMetadata dataGeneratorMetadata);
}
