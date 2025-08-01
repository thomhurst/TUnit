using System.Threading.Channels;
using TUnit.Core.Enums;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Represents the priority of a test for ordering purposes
/// </summary>
internal readonly struct TestPriority : IComparable<TestPriority>
{
    public Priority Priority { get; }
    public int Order { get; }
    
    public TestPriority(Priority priority, int order)
    {
        Priority = priority;
        Order = order;
    }
    
    public int CompareTo(TestPriority other)
    {
        var priorityComparison = ((int)other.Priority).CompareTo((int)Priority);
        if (priorityComparison != 0)
            return priorityComparison;
        
        return Order.CompareTo(other.Order);
    }
}

/// <summary>
/// A channel wrapper that maintains priority ordering of items
/// </summary>
internal class PriorityChannel<T> where T : class
{
    private readonly Channel<T> _outputChannel;
    private readonly PriorityQueue<T, TestPriority> _priorityQueue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SemaphoreSlim _itemAvailable = new(0);
    private readonly CancellationTokenSource _completionCts = new();
    private bool _isCompleted;

    public PriorityChannel(int capacity, bool singleReader = false)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = singleReader,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        };
        _outputChannel = Channel.CreateBounded<T>(options);
        _priorityQueue = new PriorityQueue<T, TestPriority>();
        _ = ProcessQueueAsync();
    }

    public ChannelReader<T> Reader => _outputChannel.Reader;

    public async ValueTask TryWriteAsync(T item, Priority priority, CancellationToken cancellationToken = default)
    {
        if (_isCompleted)
            return;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isCompleted)
                return;

            var order = int.MaxValue / 2;
            if (item is TestExecutionData testData && testData.State != null)
            {
                order = testData.State.Order;
            }
            
            var testPriority = new TestPriority(priority, order);
            _priorityQueue.Enqueue(item, testPriority);
            
        }
        finally
        {
            _semaphore.Release();
            _itemAvailable.Release();
        }
    }

    public bool TryComplete()
    {
        if (_isCompleted)
            return false;

        _isCompleted = true;
        _completionCts.Cancel();
        _itemAvailable.Release();
        return true;
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (!_completionCts.Token.IsCancellationRequested)
            {
                await _itemAvailable.WaitAsync(_completionCts.Token);
                
                await _semaphore.WaitAsync(_completionCts.Token);
                try
                {
                    if (_priorityQueue.Count > 0)
                    {
                        var item = _priorityQueue.Dequeue();
                        
                        
                        await _outputChannel.Writer.WriteAsync(item, _completionCts.Token);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await _semaphore.WaitAsync();
            try
            {
                while (_priorityQueue.Count > 0)
                {
                    var item = _priorityQueue.Dequeue();
                    _outputChannel.Writer.TryWrite(item);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            _outputChannel.Writer.TryComplete();
        }
    }
}