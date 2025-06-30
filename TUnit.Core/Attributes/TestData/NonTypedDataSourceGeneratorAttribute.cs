using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
[RequiresDynamicCode("UntypedDataSourceGeneratorAttribute requires dynamic code generation for runtime data source creation. Consider using strongly-typed AsyncDataSourceGeneratorAttribute<T> overloads for AOT compatibility.")]
[RequiresUnreferencedCode("UntypedDataSourceGeneratorAttribute may require unreferenced code for runtime data source creation. Consider using strongly-typed AsyncDataSourceGeneratorAttribute<T> overloads for AOT compatibility.")]
public abstract class UntypedDataSourceGeneratorAttribute : AsyncUntypedDataSourceGeneratorAttribute
{
    protected abstract IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata);

    protected sealed override async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var generateDataSource in GenerateDataSources(dataGeneratorMetadata))
        {
            yield return () => Task.FromResult(generateDataSource());
        }
        await Task.CompletedTask;
    }

}
