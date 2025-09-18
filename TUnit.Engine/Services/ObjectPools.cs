using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Centralized object pools for commonly allocated objects
/// </summary>
internal static class ObjectPools
{
    // Pool for List<AbstractExecutableTest>
    private static readonly ConcurrentBag<List<AbstractExecutableTest>> _testListPool = new();
    
    // Pool for HashSet<string>
    private static readonly ConcurrentBag<HashSet<string>> _stringHashSetPool = new();
    
    // Pool for List<int> used in index tracking
    private static readonly ConcurrentBag<List<int>> _intListPool = new();
    
    private const int MaxPoolSize = 100;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<AbstractExecutableTest> RentTestList(int capacity = 16)
    {
        if (_testListPool.TryTake(out var list))
        {
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
            return list;
        }
        return new List<AbstractExecutableTest>(capacity);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnTestList(List<AbstractExecutableTest> list)
    {
        if (list == null || _testListPool.Count >= MaxPoolSize)
        {
            return;
        }
        
        list.Clear();
        if (list.Capacity > 1000)
        {
            list.TrimExcess();
        }
        _testListPool.Add(list);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<string> RentStringHashSet(int capacity = 16)
    {
        if (_stringHashSetPool.TryTake(out var set))
        {
            return set;
        }
#if NETSTANDARD2_0
        return new HashSet<string>();
#else
        return new HashSet<string>(capacity);
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnStringHashSet(HashSet<string> set)
    {
        if (set == null || _stringHashSetPool.Count >= MaxPoolSize)
        {
            return;
        }
        
        set.Clear();
#if !NETSTANDARD2_0
        if (set.Count > 1000)
        {
            set.TrimExcess();
        }
#endif
        _stringHashSetPool.Add(set);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<int> RentIntList(int capacity = 16)
    {
        if (_intListPool.TryTake(out var list))
        {
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
            return list;
        }
        return new List<int>(capacity);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnIntList(List<int> list)
    {
        if (list == null || _intListPool.Count >= MaxPoolSize)
        {
            return;
        }
        
        list.Clear();
        if (list.Capacity > 1000)
        {
            list.TrimExcess();
        }
        _intListPool.Add(list);
    }
    
    /// <summary>
    /// Disposable wrapper for automatic pool return
    /// </summary>
    public struct PooledList<T> : IDisposable
    {
        private List<T> _list;
        private readonly Action<List<T>> _returnAction;
        
        public PooledList(List<T> list, Action<List<T>> returnAction)
        {
            _list = list;
            _returnAction = returnAction;
        }
        
        public List<T> List => _list;
        
        public void Dispose()
        {
            _returnAction?.Invoke(_list);
            _list = null!;
        }
    }
    
    /// <summary>
    /// Rent a test list with automatic return
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledList<AbstractExecutableTest> RentTestListScoped(int capacity = 16)
    {
        var list = RentTestList(capacity);
        return new PooledList<AbstractExecutableTest>(list, ReturnTestList);
    }
    
    /// <summary>
    /// Rent an int list with automatic return
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledList<int> RentIntListScoped(int capacity = 16)
    {
        var list = RentIntList(capacity);
        return new PooledList<int>(list, ReturnIntList);
    }
}