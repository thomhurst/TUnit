using System.Collections.Concurrent;
using System.Threading.Channels;
using TUnit.Core.Enums;
using TUnit.Engine.Logging;

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
        // Route based on priority first
        if (testData.Priority == Priority.High)
        {
            await _multiplexer.HighPriorityChannel.Writer.WriteAsync(testData, cancellationToken);
            return;
        }

        // Route based on constraints
        var constraints = testData.Constraints;
        
        if (constraints.Count == 0)
        {
            // Unconstrained test
            await _multiplexer.UnconstrainedChannel.Writer.WriteAsync(testData, cancellationToken);
        }
        else if (constraints.Contains("__global_not_in_parallel__"))
        {
            // Global NotInParallel
            await _multiplexer.GlobalNotInParallelChannel.Writer.WriteAsync(testData, cancellationToken);
        }
        else if (constraints.Any(c => c.StartsWith("__parallel_group_")))
        {
            // ParallelGroup constraint
            var groupKey = constraints.First(c => c.StartsWith("__parallel_group_"));
            var channel = _multiplexer.GetOrCreateParallelGroupChannel(groupKey);
            await channel.Writer.WriteAsync(testData, cancellationToken);
        }
        else
        {
            // Keyed NotInParallel
            var key = constraints.First(); // Use first constraint as channel key
            var channel = _multiplexer.GetOrCreateKeyedNotInParallelChannel(key);
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
            // Creating keyed NotInParallel channel
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
            // Creating ParallelGroup channel
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

    private readonly List<Channel<TestExecutionData>> _allChannelsList =
    [
    ];
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
        HighPriorityChannel.Writer.TryComplete();
        UnconstrainedChannel.Writer.TryComplete();
        GlobalNotInParallelChannel.Writer.TryComplete();
        
        foreach (var channel in _keyedNotInParallelChannels.Values)
        {
            channel.Writer.TryComplete();
        }
        
        foreach (var channel in _parallelGroupChannels.Values)
        {
            channel.Writer.TryComplete();
        }
    }
}