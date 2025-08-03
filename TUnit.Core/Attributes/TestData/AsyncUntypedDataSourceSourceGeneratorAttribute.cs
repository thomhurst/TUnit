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
        if (dataGeneratorMetadata.TestBuilderContext != null && dataGeneratorMetadata.TestInformation != null)
        {
            await PropertyInjectionService.InjectPropertiesIntoObjectAsync(this, dataGeneratorMetadata.TestBuilderContext.Current.ObjectBag, dataGeneratorMetadata.TestInformation, dataGeneratorMetadata.TestBuilderContext.Current.Events);
        }

        await ObjectInitializer.InitializeAsync(this);

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
