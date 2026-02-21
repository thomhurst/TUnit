using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class AsyncUntypedDataSourceGeneratorAttribute : Attribute, IAsyncUntypedDataSourceGeneratorAttribute
{
    /// <inheritdoc />
    public virtual bool SkipIfEmpty { get; set; }

    protected abstract IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Data source initialization is now handled externally by DataSourceInitializer
        // This follows SRP - the attribute is only responsible for generating data, not initialization
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }

    public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateAsync(dataGeneratorMetadata);
    }
}
