namespace TUnit.Engine.Scheduling;

/// <summary>
/// A work-stealing queue implementation for load balancing with optional notification support
/// </summary>
internal sealed class WorkStealingQueue<T> where T : class
{
    private readonly ConcurrentDeque<T> _items = new();
    private readonly WorkNotificationSystem? _notificationSystem;
    private volatile int _count;
    
    public WorkStealingQueue(WorkNotificationSystem? notificationSystem = null)
    {
        _notificationSystem = notificationSystem;
    }
    
    public int Count => _count;
    
    public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
    {
        _items.PushBottom(item);
        Interlocked.Increment(ref _count);
        
        // Notify workers if configured
        if (_notificationSystem != null)
        {
            await _notificationSystem.NotifyWorkAvailableAsync(
                new WorkNotification { Source = WorkNotification.WorkSource.LocalQueue },
                cancellationToken);
        }
    }
    
    public void Enqueue(T item)
    {
        _items.PushBottom(item);
        Interlocked.Increment(ref _count);
    }
    
    public bool TryDequeue(out T? item)
    {
        item = _items.TryPopBottom(out var result) ? result : null;
        if (item != null)
        {
            Interlocked.Decrement(ref _count);
            return true;
        }
        return false;
    }
    
    public bool TrySteal(out T? item)
    {
        item = _items.TryPopTop(out var result) ? result : null;
        if (item != null)
        {
            Interlocked.Decrement(ref _count);
            return true;
        }
        return false;
    }
}
