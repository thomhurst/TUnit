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
}
