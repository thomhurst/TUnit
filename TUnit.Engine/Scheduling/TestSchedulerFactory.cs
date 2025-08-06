using TUnit.Core;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Factory for creating test schedulers with various configurations
/// </summary>
internal static class TestSchedulerFactory
{
    /// <summary>
    /// Creates a scheduler with specified configuration
    /// </summary>
    public static ITestScheduler Create(SchedulerConfiguration configuration, TUnitFrameworkLogger logger, ITUnitMessageBus messageBus, EngineCancellationToken engineCancellationToken, EventReceiverOrchestrator eventReceiverOrchestrator, HookOrchestrator hookOrchestrator)
    {
        var groupingService = new TestGroupingService();

        // Use the new clean scheduler with configuration
        return new TestScheduler(
            logger,
            groupingService,
            messageBus,
            configuration);
    }
}
