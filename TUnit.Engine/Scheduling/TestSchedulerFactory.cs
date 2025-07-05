using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;

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
        var parallelismStrategy = configuration.Strategy == ParallelismStrategy.Adaptive
            ? (IParallelismStrategy) new AdaptiveParallelismStrategy(
                configuration.MinParallelism,
                configuration.MaxParallelism)
            : new FixedParallelismStrategy(configuration.MaxParallelism);

        var progressMonitor = new DefaultProgressMonitor(
            stallTimeout: configuration.StallTimeout,
            onStallDetected: message =>
            {
                logger.LogWarning(message);
                engineCancellationToken.CancellationTokenSource.Cancel();
            });

        return new DagTestScheduler(
            parallelismStrategy,
            progressMonitor,
            logger,
            configuration.TestTimeout);
    }
}
