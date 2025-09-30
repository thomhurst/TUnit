using TUnit.Core;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Defines the contract for test scheduling strategies
/// </summary>
internal interface ITestScheduler
{
    /// <summary>
    /// Schedules and executes tests with optimal parallelization
    /// </summary>
    Task ScheduleAndExecuteAsync(
        List<AbstractExecutableTest> tests,
        CancellationToken cancellationToken);
}
