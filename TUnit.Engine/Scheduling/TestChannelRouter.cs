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
        // Route based on constraints, but always pass priority for proper ordering
        var constraints = testData.Constraints;
        var priority = testData.Priority;
        
        if (constraints.Count == 0)
        {
            // Unconstrained test
            await _multiplexer.UnconstrainedChannel.TryWriteAsync(testData, priority, cancellationToken);
        }
        else if (constraints.Contains("__global_not_in_parallel__"))
        {
            // Global NotInParallel
            await _multiplexer.GlobalNotInParallelChannel.TryWriteAsync(testData, priority, cancellationToken);
        }
        else if (constraints.Any(c => c.StartsWith("__parallel_group_")))
        {
            // ParallelGroup constraint
            var groupKey = constraints.First(c => c.StartsWith("__parallel_group_"));
            var channel = _multiplexer.GetOrCreateParallelGroupChannel(groupKey);
            await channel.TryWriteAsync(testData, priority, cancellationToken);
        }
        else
        {
            // Keyed NotInParallel
            var key = constraints.First(); // Use first constraint as channel key
            var channel = _multiplexer.GetOrCreateKeyedNotInParallelChannel(key);
            await channel.TryWriteAsync(testData, priority, cancellationToken);
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
    private readonly ConcurrentDictionary<string, PriorityChannel<TestExecutionData>> _keyedNotInParallelChannels = new();
    private readonly ConcurrentDictionary<string, PriorityChannel<TestExecutionData>> _parallelGroupChannels = new();

    public PriorityChannel<TestExecutionData> UnconstrainedChannel { get; }
    public PriorityChannel<TestExecutionData> GlobalNotInParallelChannel { get; }

    public ChannelMultiplexer(TUnitFrameworkLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create priority channels with appropriate capacities
        UnconstrainedChannel = new PriorityChannel<TestExecutionData>(10000);
        GlobalNotInParallelChannel = new PriorityChannel<TestExecutionData>(1000);
        
        // Register static channels
        RegisterChannel(UnconstrainedChannel);
        RegisterChannel(GlobalNotInParallelChannel);
    }

    public PriorityChannel<TestExecutionData> GetOrCreateKeyedNotInParallelChannel(string key)
    {
        return _keyedNotInParallelChannels.GetOrAdd(key, k =>
        {
            // Creating keyed NotInParallel priority channel
            var channel = new PriorityChannel<TestExecutionData>(1000);
            RegisterChannel(channel);
            return channel;
        });
    }

    public PriorityChannel<TestExecutionData> GetOrCreateParallelGroupChannel(string groupKey)
    {
        return _parallelGroupChannels.GetOrAdd(groupKey, k =>
        {
            // Creating ParallelGroup priority channel
            var channel = new PriorityChannel<TestExecutionData>(1000);
            RegisterChannel(channel);
            return channel;
        });
    }

    private readonly List<PriorityChannel<TestExecutionData>> _allChannelsList = new();
    private readonly object _channelListLock = new object();
    
    public IEnumerable<ChannelReader<TestExecutionData>> GetAllChannelReaders()
    {
        lock (_channelListLock)
        {
            return _allChannelsList.Select(pc => pc.Reader).ToList();
        }
    }
    
    private void RegisterChannel(PriorityChannel<TestExecutionData> channel)
    {
        lock (_channelListLock)
        {
            _allChannelsList.Add(channel);
        }
    }

    public void SignalCompletion()
    {
        UnconstrainedChannel.TryComplete();
        GlobalNotInParallelChannel.TryComplete();
        
        foreach (var channel in _keyedNotInParallelChannels.Values)
        {
            channel.TryComplete();
        }
        
        foreach (var channel in _parallelGroupChannels.Values)
        {
            channel.TryComplete();
        }
    }
}