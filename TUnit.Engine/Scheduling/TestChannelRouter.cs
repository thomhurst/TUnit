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
            var hasExplicitOrder = testData.State?.Order != int.MaxValue / 2; // Check if order was explicitly set
            var channelReader = _multiplexer.GetOrCreateKeyedNotInParallelChannel(key, hasExplicitOrder);
            
            // Get the actual channel to write to
            if (hasExplicitOrder)
            {
                var sequentialChannel = _multiplexer.GetSequentialChannel(key);
                await sequentialChannel.TryWriteAsync(testData, priority, cancellationToken);
            }
            else
            {
                var priorityChannel = _multiplexer.GetPriorityChannel(key);
                await priorityChannel.TryWriteAsync(testData, priority, cancellationToken);
            }
        }
    }

    public ChannelMultiplexer GetMultiplexer() => _multiplexer;
}

internal class ChannelMultiplexer
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ConcurrentDictionary<string, SequentialChannel<TestExecutionData>> _keyedSequentialChannels = new();
    private readonly ConcurrentDictionary<string, PriorityChannel<TestExecutionData>> _keyedPriorityChannels = new();
    private readonly ConcurrentDictionary<string, PriorityChannel<TestExecutionData>> _parallelGroupChannels = new();
    private readonly ConcurrentDictionary<string, bool> _claimedSequentialChannels = new();
    private readonly ConcurrentDictionary<string, object> _channelLocks = new();
    private readonly ConcurrentDictionary<string, ChannelReader<TestExecutionData>> _keyToChannelReader = new();
    private readonly ConcurrentDictionary<ChannelReader<TestExecutionData>, string> _channelReaderToKey = new();

    public PriorityChannel<TestExecutionData> UnconstrainedChannel { get; }
    public PriorityChannel<TestExecutionData> GlobalNotInParallelChannel { get; }

    public ChannelMultiplexer(TUnitFrameworkLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create channels with appropriate capacities
        // Unconstrained tests can use priority ordering and be read by multiple consumers
        UnconstrainedChannel = new PriorityChannel<TestExecutionData>(10000, singleReader: false);
        // Global NotInParallel tests use priority ordering with single reader
        GlobalNotInParallelChannel = new PriorityChannel<TestExecutionData>(1000, singleReader: true);
        
        // Register static channels
        RegisterChannel(UnconstrainedChannel);
        RegisterChannel(GlobalNotInParallelChannel);
    }

    public ChannelReader<TestExecutionData> GetOrCreateKeyedNotInParallelChannel(string key, bool hasExplicitOrder)
    {
        if (hasExplicitOrder)
        {
            // Tests with explicit Order use sequential channel for FIFO ordering
            return _keyedSequentialChannels.GetOrAdd(key, k =>
            {
                var channel = new SequentialChannel<TestExecutionData>(1000);
                RegisterChannel(channel);
                _keyToChannelReader[k] = channel.Reader;
                _channelReaderToKey[channel.Reader] = k;
                return channel;
            }).Reader;
        }
        else
        {
            // Tests without explicit Order use priority channel
            return _keyedPriorityChannels.GetOrAdd(key, k =>
            {
                var channel = new PriorityChannel<TestExecutionData>(1000, singleReader: true);
                RegisterChannel(channel);
                _keyToChannelReader[k] = channel.Reader;
                _channelReaderToKey[channel.Reader] = k;
                return channel;
            }).Reader;
        }
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

    private readonly List<ChannelReader<TestExecutionData>> _allChannelReaders = new();
    private readonly object _channelListLock = new object();
    
    public IEnumerable<ChannelReader<TestExecutionData>> GetAllChannelReaders()
    {
        lock (_channelListLock)
        {
            return _allChannelReaders.ToList();
        }
    }
    
    private void RegisterChannel(PriorityChannel<TestExecutionData> channel)
    {
        lock (_channelListLock)
        {
            _allChannelReaders.Add(channel.Reader);
        }
    }
    
    private void RegisterChannel(SequentialChannel<TestExecutionData> channel)
    {
        lock (_channelListLock)
        {
            _allChannelReaders.Add(channel.Reader);
        }
    }

    public SequentialChannel<TestExecutionData> GetSequentialChannel(string key)
    {
        return _keyedSequentialChannels.TryGetValue(key, out var channel) 
            ? channel 
            : throw new InvalidOperationException($"Sequential channel not found for key: {key}");
    }
    
    public PriorityChannel<TestExecutionData> GetPriorityChannel(string key)
    {
        return _keyedPriorityChannels.TryGetValue(key, out var channel) 
            ? channel 
            : throw new InvalidOperationException($"Priority channel not found for key: {key}");
    }

    
    public bool TryClaimChannel(string channelKey)
    {
        return _claimedSequentialChannels.TryAdd(channelKey, true);
    }

    public void SignalCompletion()
    {
        UnconstrainedChannel.TryComplete();
        GlobalNotInParallelChannel.TryComplete();
        
        foreach (var channel in _keyedSequentialChannels.Values)
        {
            channel.TryComplete();
        }
        
        foreach (var channel in _keyedPriorityChannels.Values)
        {
            channel.TryComplete();
        }
        
        foreach (var channel in _parallelGroupChannels.Values)
        {
            channel.TryComplete();
        }
    }
    
    public void ReleaseChannelClaim(string channelKey)
    {
        _claimedSequentialChannels.TryRemove(channelKey, out _);
    }
    
    public object GetChannelLock(string channelKey)
    {
        return _channelLocks.GetOrAdd(channelKey, _ => new object());
    }
    
    public bool IsKeyedNotInParallelChannel(string channelKey)
    {
        return !channelKey.StartsWith("__parallel_group_") && !channelKey.StartsWith("__global_");
    }
    
    public ChannelReader<TestExecutionData>? TryGetChannelReaderForKey(string channelKey)
    {
        if (_keyedSequentialChannels.TryGetValue(channelKey, out var seqChannel))
            return seqChannel.Reader;
        if (_keyedPriorityChannels.TryGetValue(channelKey, out var prioChannel))
            return prioChannel.Reader;
        if (_parallelGroupChannels.TryGetValue(channelKey, out var pgChannel))
            return pgChannel.Reader;
        return null;
    }
    
    public bool IsKeyedNotInParallelChannel(ChannelReader<TestExecutionData> channelReader, out string? key)
    {
        if (_channelReaderToKey.TryGetValue(channelReader, out key))
        {
            return IsKeyedNotInParallelChannel(key);
        }
        key = null;
        return false;
    }
}