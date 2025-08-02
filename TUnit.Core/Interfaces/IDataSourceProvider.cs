namespace TUnit.Core.Interfaces;

/// <summary>
/// Core interface for any data source provider that supplies test data
/// </summary>
public interface IDataSourceProvider
{
    /// <summary>
    /// Returns the test data. Each inner object[] is a single test case.
    /// </summary>
    /// <param name="context">Context providing information about where the data is being used</param>
    /// <returns>Enumerable of test data sets</returns>
    IEnumerable<object?[]> GetData(DataSourceContext context);

    /// <summary>
    /// Indicates whether the results of GetData can be cached and reused
    /// </summary>
    bool IsCacheable { get; }
}
