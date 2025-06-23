namespace TUnit.Core.Interfaces;

/// <summary>
/// Service responsible for enumerating test data from data source providers.
/// </summary>
public interface IDataProviderService
{
    /// <summary>
    /// Gets test data from a collection of data source providers.
    /// </summary>
    /// <param name="providers">The data source providers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of test data arrays</returns>
    IAsyncEnumerable<object?[]> GetTestDataAsync(
        IEnumerable<IDataSourceProvider> providers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all test data combinations from class and method data providers.
    /// </summary>
    /// <param name="classDataProviders">Class-level data providers</param>
    /// <param name="methodDataProviders">Method-level data providers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All combinations of class and method data</returns>
    IAsyncEnumerable<(object?[] classArgs, object?[] methodArgs)> GetTestDataCombinationsAsync(
        IEnumerable<IDataSourceProvider> classDataProviders,
        IEnumerable<IDataSourceProvider> methodDataProviders,
        CancellationToken cancellationToken = default);
}