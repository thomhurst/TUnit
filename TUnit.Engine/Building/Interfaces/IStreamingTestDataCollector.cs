using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for test data collectors that support true streaming of test metadata
/// </summary>
internal interface IStreamingTestDataCollector : ITestDataCollector
{
    /// <summary>
    /// Collects test metadata as a stream, yielding items as they're discovered
    /// without buffering the entire collection
    /// </summary>
    IAsyncEnumerable<TestMetadata> CollectTestsStreamingAsync(
        string testSessionId,
        CancellationToken cancellationToken = default);
}