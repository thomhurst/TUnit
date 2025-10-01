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
    /// <returns>True if successful, false if After(TestSession) hooks failed</returns>
    Task<bool> ScheduleAndExecuteAsync(
        List<AbstractExecutableTest> tests,
        CancellationToken cancellationToken);
}
