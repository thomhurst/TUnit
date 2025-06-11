namespace TUnit.Core;

internal interface IDataSourceGeneratorAttribute : IDataAttribute
{
    internal IEnumerable<Func<object?[]?>> Generate(DataGeneratorMetadata dataGeneratorMetadata);
}

internal interface ISharedDataSourceAttribute
{
    internal IEnumerable<SharedType> GetSharedTypes();
    internal IEnumerable<string> GetKeys();
}
