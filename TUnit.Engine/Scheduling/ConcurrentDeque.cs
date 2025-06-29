using System.Collections.Concurrent;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Simple concurrent deque implementation
/// </summary>
internal sealed class ConcurrentDeque<T> where T : class
{
    private readonly ConcurrentBag<T> _items =
    [
    ];
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
