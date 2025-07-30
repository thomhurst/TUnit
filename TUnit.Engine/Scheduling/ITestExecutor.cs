using TUnit.Core;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Defines the contract for executing individual tests
/// </summary>
public interface ITestExecutor
{
    /// <summary>
    /// Executes a single test
    /// </summary>
    Task ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken);
}
