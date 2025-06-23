namespace TUnit.Core.Interfaces;

/// <summary>
/// Service responsible for expanding test metadata into executable test definitions.
/// This includes enumerating data sources, handling repeat counts, and creating test variations.
/// </summary>
public interface ITestMetadataExpander
{
    /// <summary>
    /// Expands test metadata into one or more test definitions based on data sources and repeat counts.
    /// </summary>
    /// <param name="metadata">The test metadata to expand</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An enumerable of test definitions ready for execution</returns>
    Task<IEnumerable<TestDefinition>> ExpandTestsAsync(
        ITestDescriptor metadata, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expands a test descriptor into expanded tests with all data resolved.
    /// </summary>
    /// <param name="testDescriptor">The test descriptor to expand</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of expanded tests</returns>
    IAsyncEnumerable<ExpandedTest> ExpandTestAsync(
        ITestDescriptor testDescriptor,
        CancellationToken cancellationToken = default);
}