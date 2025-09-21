using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
[RequiresDynamicCode("AsyncUntypedDataSourceGeneratorAttribute requires dynamic code generation for runtime data source creation. Consider using strongly-typed AsyncDataSourceGeneratorAttribute<T> overloads for AOT compatibility.")]
[RequiresUnreferencedCode("AsyncUntypedDataSourceGeneratorAttribute may require unreferenced code for runtime data source creation. Consider using strongly-typed AsyncDataSourceGeneratorAttribute<T> overloads for AOT compatibility.")]
public abstract class AsyncUntypedDataSourceGeneratorAttribute : Attribute, IAsyncUntypedDataSourceGeneratorAttribute
{
    protected abstract IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Data source initialization is now handled externally by DataSourceInitializer
        // This follows SRP - the attribute is only responsible for generating data, not initialization
        await foreach (var generateDataSource in GenerateDataSourcesAsync(dataGeneratorMetadata))
        {
            yield return generateDataSource;
        }
    }

    public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateAsync(dataGeneratorMetadata);
    }
}
