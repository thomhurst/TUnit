namespace TUnit.Core;

internal interface IAsyncDataSourceGeneratorAttribute : IDataAttribute
{
    internal IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata);
}