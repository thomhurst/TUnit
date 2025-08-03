namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for handling typed data sources without boxing
/// </summary>
public interface ITypedDataSourceHandler
{
    /// <summary>
    /// Checks if this handler can process the given data source
    /// </summary>
    bool CanHandle(IDataSourceAttribute dataSource);
    
    /// <summary>
    /// Gets the type that this data source provides
    /// </summary>
    Type? GetDataType(IDataSourceAttribute dataSource);
    
    /// <summary>
    /// Processes the data source and returns typed data without boxing
    /// </summary>
    Task<object?> ProcessDataSourceAsync(IDataSourceAttribute dataSource, DataGeneratorMetadata metadata);
}

/// <summary>
/// Generic interface for strongly-typed data source handlers
/// </summary>
public interface ITypedDataSourceHandler<T> : ITypedDataSourceHandler
{
    /// <summary>
    /// Processes the typed data source and returns strongly-typed data
    /// </summary>
    Task<IReadOnlyList<Func<Task<T>>>> ProcessTypedDataSourceAsync(ITypedDataSourceAttribute<T> dataSource, DataGeneratorMetadata metadata);
}