using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using LoggingExtensions = TUnit.Core.Logging.LoggingExtensions;

namespace TUnit.Engine.Scheduling;

internal class SimpleConsumerManager
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly SchedulerConfiguration _configuration;

    public SimpleConsumerManager(TUnitFrameworkLogger logger, SchedulerConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task StartConsumersAsync(
        SimpleTestQueues queues,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        Func<TestExecutionData, CancellationToken, Task> executeTestFunc,
        CancellationToken cancellationToken)
    {
        var parallelism = _configuration.Strategy == ParallelismStrategy.Adaptive
            ? _configuration.MaxParallelism  
            : _configuration.MaxParallelism;
            
        await LoggingExtensions.LogInformationAsync(_logger, $"Starting {parallelism} simple queue consumers");

        var consumerTasks = new List<Task>();
        
        // Create simple consumers that poll queues
        for (var i = 0; i < parallelism; i++)
        {
            var consumerId = i;
            consumerTasks.Add(Task.Run(async () =>
                await ConsumeFromQueuesAsync(
                    $"Consumer-{consumerId}",
                    queues,
                    runningConstraintKeys,
                    executeTestFunc,
                    cancellationToken), cancellationToken));
        }

        // Wait for all consumers to complete naturally (when queues are empty)
        await Task.WhenAll(consumerTasks);
    }

    private async Task ConsumeFromQueuesAsync(
        string consumerName,
        SimpleTestQueues queues,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        Func<TestExecutionData, CancellationToken, Task> executeTestFunc,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentConstraints = GetCurrentRunningConstraints(runningConstraintKeys);
                var testData = queues.TryDequeueTest(currentConstraints);
                
                if (testData != null)
                {
                    if (await TryAcquireConstraintsAsync(testData, runningConstraintKeys))
                    {
                        try
                        {
                            await executeTestFunc(testData, cancellationToken);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            await LoggingExtensions.LogErrorAsync(_logger, $"Error executing test {testData.Test.Context.TestName}: {ex.Message}");
                        }
                        finally
                        {
                            // Release constraints
                            foreach (var key in testData.Constraints)
                            {
                                runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
                            }
                        }
                    }
                    else
                    {
                        // Couldn't acquire constraints, put test back (this shouldn't happen with our logic)
                        await LoggingExtensions.LogWarningAsync(_logger, $"Failed to acquire constraints for test {testData.Test.Context.TestName}");
                        await Task.Delay(10, cancellationToken); // Brief delay to avoid tight loop
                    }
                }
                else
                {
                    // No work available - check if we're truly done
                    // Give a moment for any running tasks to complete and more work to appear
                    await Task.Delay(50, cancellationToken);
                    
                    // Check again after delay - if still empty and no constraints running, we're done
                    if (queues.IsEmpty() && runningConstraintKeys.All(kvp => kvp.Value == 0))
                    {
                        await LoggingExtensions.LogDebugAsync(_logger, $"{consumerName}: All queues empty and no tests running, exiting");
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await LoggingExtensions.LogDebugAsync(_logger, $"{consumerName}: Cancelled");
        }
        catch (Exception ex)
        {
            await LoggingExtensions.LogErrorAsync(_logger, $"{consumerName}: Unexpected error: {ex.Message}");
        }
    }

    private HashSet<string> GetCurrentRunningConstraints(ConcurrentDictionary<string, int> runningConstraintKeys)
    {
        return new HashSet<string>(runningConstraintKeys
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => kvp.Key));
    }

    private Task<bool> TryAcquireConstraintsAsync(
        TestExecutionData testData,
        ConcurrentDictionary<string, int> runningConstraintKeys)
    {
        var constraints = testData.Constraints;
        if (constraints.Count == 0)
        {
            return Task.FromResult(true);
        }

        var acquired = new List<string>();
        
        try
        {
            foreach (var key in constraints)
            {
                var currentCount = runningConstraintKeys.AddOrUpdate(key, 1, (k, v) => v + 1);
                
                // Check constraint violations
                var isNotInParallelConstraint = testData.State?.Constraint is NotInParallelConstraint;
                
                if (isNotInParallelConstraint && currentCount > 1)
                {
                    runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
                    break;
                }
                
                if (key.StartsWith("__parallel_group_"))
                {
                    var otherGroups = runningConstraintKeys
                        .Where(kvp => kvp.Key.StartsWith("__parallel_group_") && kvp.Key != key && kvp.Value > 0)
                        .Any();
                        
                    if (otherGroups)
                    {
                        runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
                        break;
                    }
                }
                
                acquired.Add(key);
            }

            if (acquired.Count == constraints.Count)
            {
                return Task.FromResult(true);
            }

            // Rollback partial acquisition
            foreach (var key in acquired)
            {
                runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
            }
            
            return Task.FromResult(false);
        }
        catch
        {
            // Rollback on exception
            foreach (var key in acquired)
            {
                runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
            }
            throw;
        }
    }
}