namespace TUnit.Core;

/// <summary>
/// Defines a data source that provides test data for parameterized tests.
/// Implement this interface to create custom data source attributes.
/// </summary>
public interface IDataSourceAttribute
{
    /// <summary>
    /// Asynchronously generates data rows for parameterized tests.
    /// Each yielded function, when invoked, produces one set of arguments for a test invocation.
    /// </summary>
    /// <param name="dataGeneratorMetadata">Metadata about the test and parameters being generated.</param>
    /// <returns>An async enumerable of factory functions that produce test data rows.</returns>
    public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata);

    /// <summary>
    /// When true, if the data source returns no data, the test will be skipped instead of failing.
    /// </summary>
    bool SkipIfEmpty { get; set; }
}
