using System.Collections.Concurrent;

namespace TUnit.Engine;

/// <summary>
/// Thread-safe hash set implementation using ConcurrentDictionary for better performance.
/// Provides lock-free reads and fine-grained locking for writes.
/// </summary>
internal class ConcurrentHashSet<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();

    public bool Add(T item)
    {
        return _dictionary.TryAdd(item, 0);
    }

    public void Clear()
    {
        _dictionary.Clear();
    }

    public bool Contains(T item)
    {
        return _dictionary.ContainsKey(item);
    }

    public bool Remove(T item)
    {
        return _dictionary.TryRemove(item, out _);
    }

    public int Count => _dictionary.Count;
}
