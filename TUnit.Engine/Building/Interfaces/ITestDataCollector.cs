using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for collecting test metadata from various sources (AOT or reflection)
/// </summary>
internal interface ITestDataCollector
{
    /// <summary>
    /// Collects all test metadata from the configured source
    /// </summary>
    /// <returns>Collection of test metadata ready for processing</returns>
    Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId);

    /// <summary>
    /// Collects test metadata from the configured source, optionally pre-filtering by execution filter.
    /// When a filter with extractable hints is provided, only test sources that could match are enumerated.
    /// </summary>
    /// <param name="testSessionId">The test session identifier</param>
    /// <param name="filter">Optional execution filter for pre-filtering test sources</param>
    /// <returns>Collection of test metadata ready for processing</returns>
    Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId, ITestExecutionFilter? filter);

    /// <summary>
    /// Enumerates lightweight test descriptors for fast filtering.
    /// This is the first phase of two-phase discovery, allowing filtering without full materialization.
    /// </summary>
    /// <returns>Collection of test descriptors that can be filtered before materialization.</returns>
    IEnumerable<TestDescriptor> EnumerateDescriptors();

    /// <summary>
    /// Materializes full test metadata from filtered descriptors.
    /// This is the second phase of two-phase discovery, only called for tests that passed filtering.
    /// </summary>
    /// <param name="descriptors">Descriptors that passed filtering.</param>
    /// <param name="testSessionId">The test session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of fully materialized test metadata.</returns>
    IAsyncEnumerable<TestMetadata> MaterializeFromDescriptorsAsync(
        IEnumerable<TestDescriptor> descriptors,
        string testSessionId,
        CancellationToken cancellationToken = default);
}
