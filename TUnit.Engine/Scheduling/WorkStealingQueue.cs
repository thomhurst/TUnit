namespace TUnit.Engine.Scheduling;

/// <summary>
/// A work-stealing queue implementation for load balancing
/// </summary>
public sealed class WorkStealingQueue<T> where T : class
{
    private readonly ConcurrentDeque<T> _deque = new ConcurrentDeque<T>();
    
    public void Enqueue(T item)
    {
        _deque.PushBottom(item);
    }
    
    public bool TryDequeue(out T? item)
    {
        return _deque.TryPopBottom(out item);
    }
    
    public bool TrySteal(out T? item)
    {
        return _deque.TryPopTop(out item);
    }
    
    public int Count => _deque.Count;
}