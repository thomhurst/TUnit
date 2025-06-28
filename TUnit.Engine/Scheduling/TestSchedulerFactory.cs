using TUnit.Core.Logging;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Factory for creating test schedulers with various configurations
/// </summary>
public static class TestSchedulerFactory
{
    /// <summary>
    /// Creates a scheduler with default configuration
    /// </summary>
    public static ITestScheduler CreateDefault(TUnitFrameworkLogger logger)
    {
        return Create(SchedulerConfiguration.Default, logger);
    }
    
    /// <summary>
    /// Creates a scheduler with specified configuration
    /// </summary>
    public static ITestScheduler Create(SchedulerConfiguration configuration, TUnitFrameworkLogger logger)
    {
        var parallelismStrategy = configuration.Strategy == ParallelismStrategy.Adaptive
            ? (IParallelismStrategy)new AdaptiveParallelismStrategy(
                configuration.MinParallelism,
                configuration.MaxParallelism)
            : new FixedParallelismStrategy(configuration.MaxParallelism);
            
        var progressMonitor = new DefaultProgressMonitor(
            stallTimeout: configuration.StallTimeout,
            onStallDetected: message => logger.LogWarningAsync(message).GetAwaiter().GetResult());
            
        return new DagTestScheduler(
            parallelismStrategy,
            progressMonitor,
            logger,
            configuration.TestTimeout);
    }
}