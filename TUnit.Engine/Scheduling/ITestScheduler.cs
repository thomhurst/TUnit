using TUnit.Core;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Defines the contract for test scheduling strategies
/// </summary>
public interface ITestScheduler
{
    /// <summary>
    /// Schedules and executes tests with optimal parallelization
    /// </summary>
    Task ScheduleAndExecuteAsync(
        IEnumerable<AbstractExecutableTest> tests,
        ITestExecutor executor,
        CancellationToken cancellationToken);
}
