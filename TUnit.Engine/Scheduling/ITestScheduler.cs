using System.Diagnostics.CodeAnalysis;
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
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    Task<bool> ScheduleAndExecuteAsync(
        List<AbstractExecutableTest> tests,
        CancellationToken cancellationToken);
}
