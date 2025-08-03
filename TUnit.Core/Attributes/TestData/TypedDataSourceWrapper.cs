namespace TUnit.Core;

/// <summary>
/// Wrapper for typed data sources that includes metadata for optimization
/// </summary>
public class TypedDataSourceWrapper<T> : IDataSourceAttribute, ITypedDataSourceAttribute<T>
{
    private readonly IDataSourceAttribute _innerDataSource;
    private readonly ITypedDataSourceAttribute<T>? _typedDataSource;
    
    public TypedDataSourceWrapper(IDataSourceAttribute innerDataSource)
    {
        _innerDataSource = innerDataSource;
        _typedDataSource = innerDataSource as ITypedDataSourceAttribute<T>;
    }
    
    /// <summary>
    /// The type of data this source provides
    /// </summary>
    public Type DataType { get; init; } = typeof(T);
    
    /// <summary>
    /// Whether the data type is a value type (for boxing optimization)
    /// </summary>
    public bool IsValueType { get; init; }
    
    /// <summary>
    /// Whether this data source can be optimized for the test method
    /// </summary>
    public bool CanOptimize { get; init; }
    
    public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return _innerDataSource.GetDataRowsAsync(dataGeneratorMetadata);
    }
    
    public IAsyncEnumerable<Func<Task<T>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (_typedDataSource != null)
        {
            return _typedDataSource.GetTypedDataRowsAsync(dataGeneratorMetadata);
        }
        
        // Fallback: convert from untyped data source
        return ConvertFromUntypedAsync(dataGeneratorMetadata);
    }
    
    private async IAsyncEnumerable<Func<Task<T>>> ConvertFromUntypedAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await foreach (var dataFunc in _innerDataSource.GetDataRowsAsync(dataGeneratorMetadata))
        {
            yield return async () =>
            {
                var data = await dataFunc();
                if (data?.Length == 1 && data[0] is T typedValue)
                {
                    return typedValue;
                }
                throw new InvalidCastException($"Cannot convert data source result to {typeof(T)}");
            };
        }
    }
}