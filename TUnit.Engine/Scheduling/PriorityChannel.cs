using System.Threading.Channels;
using TUnit.Core.Enums;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// A channel wrapper that maintains priority ordering of items
/// </summary>
internal class PriorityChannel<T> where T : class
{
    private readonly Channel<T> _outputChannel;
    private readonly PriorityQueue<T, int> _priorityQueue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SemaphoreSlim _itemAvailable = new(0);
    private readonly CancellationTokenSource _completionCts = new();
    private bool _isCompleted;

    public PriorityChannel(int capacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        };
        _outputChannel = Channel.CreateBounded<T>(options);
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

            // Calculate composite priority that considers both execution priority and order
            // Higher priority = more negative value (for min heap)
            // Within same priority, lower order = more negative value
            int compositePriority;
            if (item is TestExecutionData testData && testData.State != null)
            {
                // Combine priority and order: priority is more significant than order
                // Priority range: 0-5, we'll multiply by 10000 to give it more weight
                // Order typically ranges from 0 to small numbers
                // For min heap: higher priority (larger enum value) → more negative composite
                // For min heap: lower order (smaller value) → more negative composite
                compositePriority = -(int)priority * 10000 + testData.State.Order;
            }
            else
            {
                // Fallback for non-test data
                compositePriority = -(int)priority * 10000;
            }
            
            _priorityQueue.Enqueue(item, compositePriority);
        }
        finally
        {
            _semaphore.Release();
            _itemAvailable.Release(); // Signal that an item is available
        }
    }

    public bool TryComplete()
    {
        if (_isCompleted)
            return false;

        _isCompleted = true;
        _completionCts.Cancel();
        _itemAvailable.Release(); // Release any waiting thread
        return true;
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (!_completionCts.Token.IsCancellationRequested)
            {
                // Wait for an item to be available
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
            // Expected when completion is signaled
        }
        finally
        {
            // Drain any remaining items
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