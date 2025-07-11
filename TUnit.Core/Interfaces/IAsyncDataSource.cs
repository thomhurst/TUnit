namespace TUnit.Core.Interfaces;

/// <summary>
/// Async version of IDataSource for data sources that need asynchronous operations
/// </summary>
public interface IAsyncDataSource
{
    /// <summary>
    /// Returns factory functions that produce test data when invoked asynchronously.
    /// Each factory produces a single test case's arguments when called.
    /// </summary>
    /// <param name="context">Context providing information about where the data is being used</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>Async enumerable of factory functions that produce test data sets</returns>
    IAsyncEnumerable<Func<object?[]>> GenerateDataFactoriesAsync(DataSourceContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether this data source is shared across multiple tests
    /// </summary>
    bool IsShared { get; }
}

/// <summary>
/// Marker interface for async data sources that are compatible with AOT compilation.
/// </summary>
public interface IAotCompatibleAsyncDataSource : IAsyncDataSource
{
    // Marker interface - no additional members
}