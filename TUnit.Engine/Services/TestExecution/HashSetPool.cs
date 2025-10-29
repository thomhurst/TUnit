namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Thread-safe object pool for HashSet instances used during test execution.
/// Single Responsibility: Managing pooled HashSet objects to reduce allocations.
/// </summary>
internal sealed class HashSetPool
{
    private readonly Dictionary<Type, object> _pools = new();
    private readonly object _lock = new();

    public HashSet<T> Rent<T>()
    {
        var type = typeof(T);

        lock (_lock)
        {
            if (!_pools.TryGetValue(type, out var poolObj))
            {
                poolObj = new Stack<HashSet<T>>();
                _pools[type] = poolObj;
            }

            var pool = (Stack<HashSet<T>>)poolObj;

            if (pool.Count > 0)
            {
                var set = pool.Pop();
                set.Clear();
                return set;
            }
        }

        return [];
    }

    public void Return<T>(HashSet<T> set)
    {
        var type = typeof(T);

        lock (_lock)
        {
            set.Clear();

            if (!_pools.TryGetValue(type, out var poolObj))
            {
                poolObj = new Stack<HashSet<T>>();
                _pools[type] = poolObj;
            }

            var pool = (Stack<HashSet<T>>)poolObj;
            pool.Push(set);
        }
    }
}
