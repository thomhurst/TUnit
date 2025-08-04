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
                var testData = queues.TryDequeueTestWithConstraints(currentConstraints, runningConstraintKeys, _constraintLock);
                
                if (testData != null)
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

    private readonly object _constraintLock = new object();

    private Task<bool> TryAcquireConstraintsAsync(
        TestExecutionData testData,
        ConcurrentDictionary<string, int> runningConstraintKeys)
    {
        var constraints = testData.Constraints;
        if (constraints.Count == 0)
        {
            return Task.FromResult(true);
        }

        lock (_constraintLock)
        {
            var acquired = new List<string>();
            
            try
            {
                // First, check if we can acquire ALL constraints without actually acquiring them
                foreach (var key in constraints)
                {
                    runningConstraintKeys.TryGetValue(key, out var currentCount);
                    var isNotInParallelConstraint = testData.State?.Constraint is NotInParallelConstraint;
                    
                    if (isNotInParallelConstraint && currentCount > 0)
                    {
                        // Another NotInParallel test is already running with this key
                        return Task.FromResult(false);
                    }
                    
                    if (key.StartsWith("__parallel_group_"))
                    {
                        var otherGroups = runningConstraintKeys
                            .Where(kvp => kvp.Key.StartsWith("__parallel_group_") && kvp.Key != key && kvp.Value > 0)
                            .Any();
                            
                        if (otherGroups)
                        {
                            // Another parallel group is running
                            return Task.FromResult(false);
                        }
                    }
                }
                
                // If we get here, we can acquire all constraints
                foreach (var key in constraints)
                {
                    runningConstraintKeys.AddOrUpdate(key, 1, (k, v) => v + 1);
                    acquired.Add(key);
                }

                return Task.FromResult(true);
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
}