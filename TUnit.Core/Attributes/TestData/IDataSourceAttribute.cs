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

    /// <summary>
    /// When <c>true</c>, this data source is not enumerated during test discovery. Instead, the test
    /// appears as a single placeholder node, and the data rows are enumerated and executed at runtime
    /// (each reported as a result nested under the placeholder). This avoids the IDE/test-explorer
    /// overhead of expanding a data source that produces a large number of cases.
    /// </summary>
    /// <remarks>
    /// If any data source on a test sets this to <c>true</c>, the entire test's case expansion is
    /// deferred to runtime. Tests deferred this way cannot be targeted individually by a filter, and
    /// other tests cannot <c>[DependsOn]</c> their rows (the rows do not exist until runtime).
    /// </remarks>
    bool DeferEnumeration { get; set; }
}
