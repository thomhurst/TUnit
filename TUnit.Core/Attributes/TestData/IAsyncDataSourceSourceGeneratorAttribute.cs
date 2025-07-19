namespace TUnit.Core;

public interface IAsyncDataSourceGeneratorAttribute : IDataSourceAttribute, IRequiresImmediateInitialization
{
    IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata);
}
