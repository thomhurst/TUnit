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
    /// Creates a scheduler with default configuration
    /// </summary>
    public static ITestScheduler CreateDefault(TUnitFrameworkLogger logger, EngineCancellationToken engineCancellationToken)
    {
        return Create(SchedulerConfiguration.Default, logger, engineCancellationToken);
    }

    /// <summary>
    /// Creates a scheduler with specified configuration
    /// </summary>
    public static ITestScheduler Create(SchedulerConfiguration configuration, TUnitFrameworkLogger logger, EngineCancellationToken engineCancellationToken)
    {
        var groupingService = new TestGroupingService();
        
        return new ProducerConsumerTestScheduler(
            logger,
            groupingService,
            configuration);
    }
}
