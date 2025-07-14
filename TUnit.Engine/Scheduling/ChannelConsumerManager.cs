using System.Collections.Concurrent;
using System.Threading.Channels;
using TUnit.Engine.Execution;
using TUnit.Engine.Logging;
using LoggingExtensions = TUnit.Core.Logging.LoggingExtensions;

namespace TUnit.Engine.Scheduling;

internal class ChannelConsumerManager
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly SchedulerConfiguration _configuration;
    private readonly List<Task> _consumerTasks = new();
    private readonly CancellationTokenSource _shutdownCts = new();

    public ChannelConsumerManager(TUnitFrameworkLogger logger, SchedulerConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task StartConsumersAsync(
        ITestExecutor executor,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        Func<TestExecutionData, CancellationToken, Task> executeTestFunc,
        ChannelMultiplexer multiplexer,
        CancellationToken cancellationToken)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdownCts.Token);

        // Start consumers based on parallelism strategy
        var parallelism = _configuration.Strategy == ParallelismStrategy.Adaptive
            ? _configuration.MaxParallelism  // Use max for better performance
            : _configuration.MaxParallelism;
            
        await LoggingExtensions.LogInformationAsync(_logger, $"Starting {parallelism} universal work-stealing consumers");

        // Create universal work-stealing consumers that can pull from ALL channels
        for (int i = 0; i < parallelism; i++)
        {
            var consumerId = i;
            _consumerTasks.Add(Task.Run(async () =>
                await ConsumeUniversalWorkStealingAsync(
                    $"Universal-{consumerId}",
                    multiplexer,
                    runningConstraintKeys,
                    executeTestFunc,
                    linkedCts.Token), linkedCts.Token));
        }

        // Wait for all consumers to complete
        await Task.WhenAll(_consumerTasks);
    }

    private async Task ConsumeUniversalWorkStealingAsync(
        string consumerName,
        ChannelMultiplexer multiplexer,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        Func<TestExecutionData, CancellationToken, Task> executeTestFunc,
        CancellationToken cancellationToken)
    {
        // Consumer started

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var foundWork = false;
                TestExecutionData? testData = null;

                // Priority order: High Priority -> Unconstrained -> Global NotInParallel -> Others
                if (multiplexer.HighPriorityChannel.Reader.TryRead(out testData))
                {
                    foundWork = true;
                    // Work found in HighPriorityChannel
                }
                else if (multiplexer.UnconstrainedChannel.Reader.TryRead(out testData))
                {
                    foundWork = true;
                    // Work found in UnconstrainedChannel
                }
                else if (multiplexer.GlobalNotInParallelChannel.Reader.TryRead(out testData))
                {
                    foundWork = true;
                    // Work found in GlobalNotInParallelChannel
                }
                else
                {
                    // Try all dynamic channels (get fresh list each time to catch new channels)
                    foreach (var channel in multiplexer.GetAllChannels())
                    {
                        if (channel != multiplexer.HighPriorityChannel && 
                            channel != multiplexer.UnconstrainedChannel && 
                            channel != multiplexer.GlobalNotInParallelChannel &&
                            channel.Reader.TryRead(out testData))
                        {
                            foundWork = true;
                            // Work found in dynamic channel
                            break;
                        }
                    }
                }

                if (foundWork && testData != null)
                {
                    if (await TryAcquireConstraintsAsync(testData, runningConstraintKeys, cancellationToken))
                    {
                        try
                        {
                            await executeTestFunc(testData, cancellationToken);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            await LoggingExtensions.LogErrorAsync(_logger, $"Error executing test {testData.Test.Context.TestName}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Just yield and try again later - constraint will be released soon
                        await Task.Yield();
                    }
                }
                else
                {
                    // No work available, check if all channels are completed
                    var allChannels = multiplexer.GetAllChannels().Select(c => c.Reader).ToList();
                    var activeChannels = allChannels.Where(c => !c.Completion.IsCompleted).ToList();
                    
                    // No work found, checking channel completion
                    
                    if (activeChannels.Count == 0)
                    {
                        // All channels completed, exit
                        // All channels completed, consumer exiting
                        break;
                    }
                    
                    // Wait for any active channel to have data or complete
                    var waitTasks = activeChannels
                        .Select(c => c.WaitToReadAsync(cancellationToken).AsTask())
                        .ToArray();
                    
                    if (waitTasks.Length > 0)
                    {
                        try
                        {
                            // Waiting for channels
                            await Task.WhenAny(waitTasks);
                        }
                        catch (ChannelClosedException)
                        {
                            // Channel closed, continue to check other channels
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            // Cancellation requested, exit
                            break;
                        }
                    }
                    else
                    {
                        // No active channels, all must be completed
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Consumer cancelled
        }
        finally
        {
            // Consumer stopped
        }
    }

    private Task<bool> TryAcquireConstraintsAsync(
        TestExecutionData testData,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        CancellationToken cancellationToken)
    {
        var constraints = testData.Constraints;
        if (constraints.Count == 0)
        {
            return Task.FromResult(true);
        }

        // Try to acquire all constraint keys atomically
        var acquired = new List<string>();
        
        try
        {
            foreach (var key in constraints)
            {
                var currentCount = runningConstraintKeys.AddOrUpdate(key, 1, (k, v) => v + 1);
                
                // For NotInParallel constraints, only one test can run at a time
                if (key.Contains("__not_in_parallel__") && currentCount > 1)
                {
                    // Release this key and all previously acquired keys
                    runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
                    break;
                }
                
                // For ParallelGroup constraints, allow multiple tests from same group
                // but prevent tests from different groups
                if (key.StartsWith("__parallel_group_"))
                {
                    // Check if any other parallel group is running
                    var otherGroups = runningConstraintKeys
                        .Where(kvp => kvp.Key.StartsWith("__parallel_group_") && kvp.Key != key && kvp.Value > 0)
                        .Any();
                        
                    if (otherGroups)
                    {
                        // Release this key and all previously acquired keys
                        runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
                        break;
                    }
                }
                
                acquired.Add(key);
            }

            // If we acquired all constraints, we're good to go
            if (acquired.Count == constraints.Count)
            {
                return Task.FromResult(true);
            }

            // Otherwise, release all acquired constraints
            foreach (var key in acquired)
            {
                runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
            }
            
            return Task.FromResult(false);
        }
        catch
        {
            // On error, release all acquired constraints
            foreach (var key in acquired)
            {
                runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
            }
            throw;
        }
    }

    public void Shutdown()
    {
        _shutdownCts.Cancel();
    }
}