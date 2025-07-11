using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Provides streaming test discovery capabilities
/// </summary>
internal interface IStreamingTestDiscovery
{
    /// <summary>
    /// Discovers tests as a stream, enabling parallel discovery and execution
    /// </summary>
    IAsyncEnumerable<ExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId, 
        CancellationToken cancellationToken = default);
}