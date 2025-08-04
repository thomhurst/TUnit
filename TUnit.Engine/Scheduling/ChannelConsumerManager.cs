using System.Collections.Concurrent;
using System.Threading.Channels;
using TUnit.Core;
using TUnit.Engine.Logging;
using LoggingExtensions = TUnit.Core.Logging.LoggingExtensions;

namespace TUnit.Engine.Scheduling;

internal class ChannelConsumerManager
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly SchedulerConfiguration _configuration;
    private readonly List<Task> _consumerTasks =
    [
    ];
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
        for (var i = 0; i < parallelism; i++)
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
        ChannelReader<TestExecutionData>? dedicatedChannel = null;
        string? dedicatedChannelKey = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var foundWork = false;
                TestExecutionData? testData = null;

                if (dedicatedChannel != null)
                {
                    if (dedicatedChannel.TryRead(out testData))
                    {
                        foundWork = true;
                    }
                    else if (dedicatedChannel.Completion.IsCompleted)
                    {
                        if (dedicatedChannelKey != null)
                        {
                            multiplexer.ReleaseChannelClaim(dedicatedChannelKey);
                        }
                        dedicatedChannel = null;
                        dedicatedChannelKey = null;
                    }
                }
                else
                {
                    if (multiplexer.UnconstrainedChannel.Reader.TryRead(out testData))
                    {
                        foundWork = true;
                    }
                    else if (multiplexer.GlobalNotInParallelChannel.Reader.TryRead(out testData))
                    {
                        foundWork = true;
                        if (dedicatedChannel == null && multiplexer.TryClaimChannel("__global_not_in_parallel__"))
                        {
                            dedicatedChannel = multiplexer.GlobalNotInParallelChannel.Reader;
                            dedicatedChannelKey = "__global_not_in_parallel__";
                        }
                    }
                    else
                    {
                        foreach (var channelReader in multiplexer.GetAllChannelReaders())
                        {
                            if (channelReader != multiplexer.UnconstrainedChannel.Reader && 
                                channelReader != multiplexer.GlobalNotInParallelChannel.Reader)
                            {
                                if (multiplexer.IsKeyedNotInParallelChannel(channelReader, out var channelKey) && channelKey != null)
                                {
                                    if (dedicatedChannelKey != channelKey)
                                    {
                                        if (!multiplexer.TryClaimChannel(channelKey))
                                        {
                                            continue;
                                        }
                                        dedicatedChannel = channelReader;
                                        dedicatedChannelKey = channelKey;
                                        
                                    }
                                    
                                    if (dedicatedChannel == channelReader && channelReader.TryRead(out testData))
                                    {
                                        foundWork = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    if (channelReader.TryRead(out testData))
                                    {
                                        foundWork = true;
                                        break;
                                    }
                                }
                            }
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
                        await Task.Yield();
                    }
                }
                else
                {
                    var allChannelReaders = multiplexer.GetAllChannelReaders().ToList();
                    var activeChannels = allChannelReaders.Where(c => !c.Completion.IsCompleted).ToList();
                    
                    if (activeChannels.Count == 0)
                    {
                        break;
                    }
                    
                    var waitTasks = activeChannels
                        .Select(c => c.WaitToReadAsync(cancellationToken).AsTask())
                        .ToArray();
                    
                    if (waitTasks.Length > 0)
                    {
                        try
                        {
                            await Task.WhenAny(waitTasks);
                        }
                        catch (ChannelClosedException)
                        {
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
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

        var acquired = new List<string>();
        
        try
        {
            foreach (var key in constraints)
            {
                var currentCount = runningConstraintKeys.AddOrUpdate(key, 1, (k, v) => v + 1);
                
                // Check if this is a NotInParallel constraint (either global or keyed)
                // Global NotInParallel uses "__global_not_in_parallel__" key
                // Keyed NotInParallel uses the actual key value (e.g., "1", "3", "MyKey")
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

            foreach (var key in acquired)
            {
                runningConstraintKeys.AddOrUpdate(key, 0, (k, v) => Math.Max(0, v - 1));
            }
            
            return Task.FromResult(false);
        }
        catch
        {
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