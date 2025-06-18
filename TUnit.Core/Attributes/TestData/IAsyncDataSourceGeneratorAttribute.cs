namespace TUnit.Core;

public interface IAsyncDataSourceGeneratorAttribute : IDataAttribute, IRequiresImmediateInitialization
{
    IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata);
}
