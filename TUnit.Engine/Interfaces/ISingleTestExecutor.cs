using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for executing a single test
/// </summary>
public interface ISingleTestExecutor
{
    void SetSessionId(SessionUid sessionUid);
    Task<TestNodeUpdateMessage> ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken);
}
