using System.Collections.Concurrent;
using System.Threading.Channels;
using TUnit.Core.Enums;
using TUnit.Engine.Logging;
using LoggingExtensions = TUnit.Core.Logging.LoggingExtensions;

namespace TUnit.Engine.Scheduling;

internal class TestChannelRouter
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ChannelMultiplexer _multiplexer;

    public TestChannelRouter(TUnitFrameworkLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _multiplexer = new ChannelMultiplexer(logger);
    }

    public async Task RouteTestAsync(TestExecutionData testData, CancellationToken cancellationToken)
    {
        await LoggingExtensions.LogDebugAsync(_logger, $"RouteTestAsync: {testData.Test.Context.TestName} with priority {testData.Priority}, constraints: [{string.Join(", ", testData.Constraints)}]");
        
        // Route based on priority first
        if (testData.Priority == Priority.High)
        {
            await LoggingExtensions.LogDebugAsync(_logger, $"Routing to HighPriorityChannel: {testData.Test.Context.TestName}");
            await _multiplexer.HighPriorityChannel.Writer.WriteAsync(testData, cancellationToken);
            return;
        }

        // Route based on constraints
        var constraints = testData.Constraints;
        
        if (constraints.Count == 0)
        {
            // Unconstrained test
            await LoggingExtensions.LogDebugAsync(_logger, $"Routing to UnconstrainedChannel: {testData.Test.Context.TestName}");
            await _multiplexer.UnconstrainedChannel.Writer.WriteAsync(testData, cancellationToken);
        }
        else if (constraints.Contains("__global_not_in_parallel__"))
        {
            // Global NotInParallel
            await LoggingExtensions.LogDebugAsync(_logger, $"Routing to GlobalNotInParallelChannel: {testData.Test.Context.TestName}");
            await _multiplexer.GlobalNotInParallelChannel.Writer.WriteAsync(testData, cancellationToken);
        }
        else if (constraints.Any(c => c.StartsWith("__parallel_group_")))
        {
            // ParallelGroup constraint
            var groupKey = constraints.First(c => c.StartsWith("__parallel_group_"));
            var channel = _multiplexer.GetOrCreateParallelGroupChannel(groupKey);
            await LoggingExtensions.LogDebugAsync(_logger, $"Routing to ParallelGroupChannel ({groupKey}): {testData.Test.Context.TestName}");
            await channel.Writer.WriteAsync(testData, cancellationToken);
        }
        else
        {
            // Keyed NotInParallel
            var key = constraints.First(); // Use first constraint as channel key
            var channel = _multiplexer.GetOrCreateKeyedNotInParallelChannel(key);
            await LoggingExtensions.LogDebugAsync(_logger, $"Routing to KeyedNotInParallelChannel ({key}): {testData.Test.Context.TestName}");
            await channel.Writer.WriteAsync(testData, cancellationToken);
        }
    }

    public ChannelMultiplexer GetMultiplexer() => _multiplexer;

    public void SignalCompletion()
    {
        _multiplexer.SignalCompletion();
    }
}

internal class ChannelMultiplexer
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ConcurrentDictionary<string, Channel<TestExecutionData>> _keyedNotInParallelChannels = new();
    private readonly ConcurrentDictionary<string, Channel<TestExecutionData>> _parallelGroupChannels = new();

    public Channel<TestExecutionData> HighPriorityChannel { get; }
    public Channel<TestExecutionData> UnconstrainedChannel { get; }
    public Channel<TestExecutionData> GlobalNotInParallelChannel { get; }

    public ChannelMultiplexer(TUnitFrameworkLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create channels with appropriate bounds
        var highPriorityOptions = new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        // Larger channel sizes for better throughput
        var unconstrainedOptions = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = true // Allow sync continuations for better performance
        };

        var constrainedOptions = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false, // Allow multiple readers for better parallelism
            SingleWriter = false,
            AllowSynchronousContinuations = true
        };

        HighPriorityChannel = Channel.CreateUnbounded<TestExecutionData>(highPriorityOptions);
        UnconstrainedChannel = Channel.CreateBounded<TestExecutionData>(unconstrainedOptions);
        GlobalNotInParallelChannel = Channel.CreateBounded<TestExecutionData>(constrainedOptions);
        
        // Register static channels
        RegisterChannel(HighPriorityChannel);
        RegisterChannel(UnconstrainedChannel);
        RegisterChannel(GlobalNotInParallelChannel);
    }

    public Channel<TestExecutionData> GetOrCreateKeyedNotInParallelChannel(string key)
    {
        return _keyedNotInParallelChannels.GetOrAdd(key, k =>
        {
            _ = LoggingExtensions.LogDebugAsync(_logger, $"Creating keyed NotInParallel channel for key: {k}");
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = true
            };
            var channel = Channel.CreateBounded<TestExecutionData>(options);
            RegisterChannel(channel);
            return channel;
        });
    }

    public Channel<TestExecutionData> GetOrCreateParallelGroupChannel(string groupKey)
    {
        return _parallelGroupChannels.GetOrAdd(groupKey, k =>
        {
            _ = LoggingExtensions.LogDebugAsync(_logger, $"Creating ParallelGroup channel for group: {k}");
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false, // Multiple consumers allowed within a group
                SingleWriter = false,
                AllowSynchronousContinuations = true
            };
            var channel = Channel.CreateBounded<TestExecutionData>(options);
            RegisterChannel(channel);
            return channel;
        });
    }

    private readonly List<Channel<TestExecutionData>> _allChannelsList = new();
    private readonly object _channelListLock = new object();
    
    public IEnumerable<Channel<TestExecutionData>> GetAllChannels()
    {
        lock (_channelListLock)
        {
            return _allChannelsList.ToList();
        }
    }
    
    private void RegisterChannel(Channel<TestExecutionData> channel)
    {
        lock (_channelListLock)
        {
            _allChannelsList.Add(channel);
        }
    }

    public void SignalCompletion()
    {
        _ = LoggingExtensions.LogDebugAsync(_logger, "Signaling completion to all channels");
        
        var completedChannels = 0;
        
        if (HighPriorityChannel.Writer.TryComplete())
            completedChannels++;
        if (UnconstrainedChannel.Writer.TryComplete())
            completedChannels++;
        if (GlobalNotInParallelChannel.Writer.TryComplete())
            completedChannels++;
        
        foreach (var channel in _keyedNotInParallelChannels.Values)
        {
            if (channel.Writer.TryComplete())
                completedChannels++;
        }
        
        foreach (var channel in _parallelGroupChannels.Values)
        {
            if (channel.Writer.TryComplete())
                completedChannels++;
        }
        
        _ = LoggingExtensions.LogDebugAsync(_logger, $"Completed {completedChannels} channels");
    }
}