using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine;

/// <summary>
/// Interface for test execution
/// </summary>
public interface ITestExecutor
{
    Task ExecuteTests(
        IEnumerable<ExecutableTest> tests,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken);
}
