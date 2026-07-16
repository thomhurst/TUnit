using System.Collections.Concurrent;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Thread-safe object pool for HashSet instances used during test execution.
/// Single Responsibility: Managing pooled HashSet objects to reduce allocations.
/// Uses lock-free concurrent collections for high-performance parallel test execution.
/// </summary>
internal sealed class HashSetPool
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _pools = new();

    public HashSet<T> Rent<T>()
    {
        var type = typeof(T);
        var bag = _pools.GetOrAdd(type, _ => new ConcurrentBag<object>());

        if (bag.TryTake(out var pooledSet))
        {
            var set = (HashSet<T>)pooledSet;
            set.Clear();
            return set;
        }

        return [];
    }

    public void Return<T>(HashSet<T> set)
    {
        var type = typeof(T);
        set.Clear();

        var bag = _pools.GetOrAdd(type, _ => new ConcurrentBag<object>());
        bag.Add(set);
    }
}
