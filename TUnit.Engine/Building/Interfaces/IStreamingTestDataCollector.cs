using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for test data collectors that support streaming discovery
/// </summary>
internal interface IStreamingTestDataCollector : ITestDataCollector
{
    /// <summary>
    /// Collects test metadata as a stream for immediate processing
    /// </summary>
    IAsyncEnumerable<TestMetadata> CollectTestsStreamAsync(
        string testSessionId, 
        CancellationToken cancellationToken = default);
}