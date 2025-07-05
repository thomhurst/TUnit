namespace TUnit.Core;

public interface IAsyncDataSourceGeneratorAttribute : IDataAttribute
{
    IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata);
}