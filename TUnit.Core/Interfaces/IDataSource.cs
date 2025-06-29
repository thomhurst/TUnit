namespace TUnit.Core.Interfaces;

/// <summary>
/// Core interface for any data source that supplies test data as factories
/// </summary>
public interface IDataSource
{
    /// <summary>
    /// Returns factory functions that produce test data when invoked.
    /// Each factory produces a single test case's arguments when called.
    /// </summary>
    /// <param name="context">Context providing information about where the data is being used</param>
    /// <returns>Enumerable of factory functions that produce test data sets</returns>
    IEnumerable<Func<object?[]>> GenerateDataFactories(DataSourceContext context);
    
    /// <summary>
    /// Indicates whether this data source is shared across multiple tests
    /// </summary>
    bool IsShared { get; }
}

/// <summary>
/// Marker interface for data sources that are compatible with AOT compilation.
/// A data source implementing this interface must not use reflection or other dynamic
/// features that prevent AOT compilation within its GenerateDataFactories() method.
/// </summary>
public interface IAotCompatibleDataSource : IDataSource 
{
    // Marker interface - no additional members
}