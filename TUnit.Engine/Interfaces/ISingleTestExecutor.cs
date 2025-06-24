using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Engine.Logging;

namespace TUnit.Engine;

/// <summary>
/// Interface for executing a single test
/// </summary>
public interface ISingleTestExecutor
{
    void SetSessionId(SessionUid sessionUid);
    Task<TestNodeUpdateMessage> ExecuteTestAsync(ExecutableTest test, IMessageBus messageBus, CancellationToken cancellationToken);
}