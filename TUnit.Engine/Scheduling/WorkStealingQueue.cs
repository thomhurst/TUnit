using System.Collections.Concurrent;

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

/// <summary>
/// Simple concurrent deque implementation
/// </summary>
internal sealed class ConcurrentDeque<T> where T : class
{
    private readonly ConcurrentBag<T> _items = new ConcurrentBag<T>();
    private int _count;
    
    public void PushBottom(T item)
    {
        _items.Add(item);
        Interlocked.Increment(ref _count);
    }
    
    public bool TryPopBottom(out T? item)
    {
        if (_items.TryTake(out item))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }
        return false;
    }
    
    public bool TryPopTop(out T? item)
    {
        // For simplicity, using same as TryPopBottom
        // A full implementation would maintain proper deque semantics
        return TryPopBottom(out item);
    }
    
    public int Count => _count;
}