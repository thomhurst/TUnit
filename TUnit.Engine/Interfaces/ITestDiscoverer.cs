using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine;

/// <summary>
/// Interface for test discovery
/// </summary>
public interface ITestDiscoverer
{
    Task<IEnumerable<TestNode>> DiscoverTestsAsync(
        DiscoverTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken);
}