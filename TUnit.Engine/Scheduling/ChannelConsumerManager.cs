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
    private TestChannelRouter? _channelRouter;

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
        TestChannelRouter channelRouter,
        CancellationToken cancellationToken)
    {
        _channelRouter = channelRouter;
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
        
        // Add one dedicated consumer for GlobalNotInParallel to ensure it's always processed
        _consumerTasks.Add(Task.Run(async () =>
            await ConsumeChannelAsync(
                "GlobalNotInParallel",
                multiplexer.GlobalNotInParallelChannel.Reader,
                runningConstraintKeys,
                executeTestFunc,
                linkedCts.Token), linkedCts.Token));

        // Wait for all consumers to complete
        await Task.WhenAll(_consumerTasks);
    }

    private async Task ConsumeChannelAsync(
        string consumerName,
        ChannelReader<TestExecutionData> reader,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        Func<TestExecutionData, CancellationToken, Task> executeTestFunc,
        CancellationToken cancellationToken)
    {
        await LoggingExtensions.LogDebugAsync(_logger, $"Consumer {consumerName} started");

        try
        {
            await foreach (var testData in reader.ReadAllAsync(cancellationToken))
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
                    // Yield to let other work proceed, then retry
                    await Task.Yield();
                    // Put it back on the channel
                    // Can't write to reader - need to route back through the router
                    if (_channelRouter != null)
                    {
                        await _channelRouter.RouteTestAsync(testData, cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await LoggingExtensions.LogDebugAsync(_logger, $"Consumer {consumerName} cancelled");
        }
        finally
        {
            await LoggingExtensions.LogDebugAsync(_logger, $"Consumer {consumerName} stopped");
        }
    }

    private async Task ConsumeUniversalWorkStealingAsync(
        string consumerName,
        ChannelMultiplexer multiplexer,
        ConcurrentDictionary<string, int> runningConstraintKeys,
        Func<TestExecutionData, CancellationToken, Task> executeTestFunc,
        CancellationToken cancellationToken)
    {
        await LoggingExtensions.LogDebugAsync(_logger, $"Universal work-stealing consumer {consumerName} started");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var foundWork = false;
                TestExecutionData? testData = null;

                // Priority order: High Priority -> Unconstrained -> Global NotInParallel -> Others
                if (multiplexer.HighPriorityChannel.Reader.TryRead(out testData) ||
                    multiplexer.UnconstrainedChannel.Reader.TryRead(out testData) ||
                    multiplexer.GlobalNotInParallelChannel.Reader.TryRead(out testData))
                {
                    foundWork = true;
                }
                else
                {
                    // Try all dynamic channels
                    foreach (var channel in multiplexer.GetAllChannels())
                    {
                        if (channel.Reader.TryRead(out testData))
                        {
                            foundWork = true;
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
                        // Put the test back for another consumer to try
                        // This is a simple spin-wait approach
                        await Task.Yield();
                        // Try to route it again
                        if (_channelRouter != null)
                        {
                            await _channelRouter.RouteTestAsync(testData, cancellationToken);
                        }
                    }
                }
                else
                {
                    // No work available, use async wait to avoid busy waiting
                    var allChannels = new List<ChannelReader<TestExecutionData>>
                    {
                        multiplexer.HighPriorityChannel.Reader,
                        multiplexer.UnconstrainedChannel.Reader,
                        multiplexer.GlobalNotInParallelChannel.Reader
                    };
                    allChannels.AddRange(multiplexer.GetAllChannels().Select(c => c.Reader));
                    
                    var waitTasks = allChannels
                        .Where(c => !c.Completion.IsCompleted)
                        .Select(c => c.WaitToReadAsync(cancellationToken).AsTask())
                        .ToArray();
                    
                    if (waitTasks.Length == 0)
                    {
                        // All channels completed
                        break;
                    }
                    
                    try
                    {
                        await Task.WhenAny(waitTasks);
                    }
                    catch (ChannelClosedException)
                    {
                        // Continue to check other channels
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await LoggingExtensions.LogDebugAsync(_logger, $"Universal work-stealing consumer {consumerName} cancelled");
        }
        finally
        {
            await LoggingExtensions.LogDebugAsync(_logger, $"Universal work-stealing consumer {consumerName} stopped");
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