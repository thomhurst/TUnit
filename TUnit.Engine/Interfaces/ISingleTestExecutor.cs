using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for executing a single test
/// </summary>
public interface ISingleTestExecutor
{
    Task<TestNodeUpdateMessage> ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken);
}
