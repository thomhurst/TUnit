using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// High-performance object pools for frequently allocated objects
/// </summary>
internal static class ObjectPools
{
    /// <summary>
    /// Generic object pool implementation
    /// </summary>
    private class ObjectPool<T> where T : class, new()
    {
        private readonly ConcurrentQueue<T> _objects = new();
        private readonly Func<T> _objectGenerator;
        private readonly Action<T>? _resetAction;
        private int _currentCount = 0;
        private readonly int _maxSize;

        public ObjectPool(int maxSize = 64, Func<T>? objectGenerator = null, Action<T>? resetAction = null)
        {
            _maxSize = maxSize;
            _objectGenerator = objectGenerator ?? (() => new T());
            _resetAction = resetAction;
        }

        public T Get()
        {
            if (_objects.TryDequeue(out var item))
            {
                Interlocked.Decrement(ref _currentCount);
                return item;
            }
            return _objectGenerator();
        }

        public void Return(T item)
        {
            if (_currentCount >= _maxSize)
            {
                return; // Pool is full, let GC handle it
            }

            _resetAction?.Invoke(item);
            _objects.Enqueue(item);
            Interlocked.Increment(ref _currentCount);
        }
    }

    // Specific pools for high-frequency objects
    private static readonly ObjectPool<TestResult> _testResultPool = new(
        maxSize: 128,
        resetAction: result =>
        {
            result.State = TestState.None;
            result.Exception = null;
            result.Start = null;
            result.End = null;
            result.Duration = TimeSpan.Zero;
            result.ComputerName = null;
            result.Output = null;
            result.OutputAttachments.Clear();
            result.TestContext = null;
            result.OverrideReason = null;
        });

    private static readonly ObjectPool<List<Exception>> _exceptionListPool = new(
        maxSize: 64,
        resetAction: list => list.Clear());

    private static readonly ObjectPool<List<string>> _stringListPool = new(
        maxSize: 64,
        resetAction: list => list.Clear());

    private static readonly ObjectPool<StringBuilder> _stringBuilderPool = new(
        maxSize: 32,
        resetAction: sb => sb.Clear());

    /// <summary>
    /// Gets a pooled TestResult instance
    /// </summary>
    public static TestResult GetTestResult()
    {
        return _testResultPool.Get();
    }

    /// <summary>
    /// Returns a TestResult instance to the pool
    /// </summary>
    public static void ReturnTestResult(TestResult result)
    {
        _testResultPool.Return(result);
    }

    /// <summary>
    /// Gets a pooled List&lt;Exception&gt; instance
    /// </summary>
    public static List<Exception> GetExceptionList()
    {
        return _exceptionListPool.Get();
    }

    /// <summary>
    /// Returns a List&lt;Exception&gt; instance to the pool
    /// </summary>
    public static void ReturnExceptionList(List<Exception> list)
    {
        _exceptionListPool.Return(list);
    }

    /// <summary>
    /// Gets a pooled List&lt;string&gt; instance
    /// </summary>
    public static List<string> GetStringList()
    {
        return _stringListPool.Get();
    }

    /// <summary>
    /// Returns a List&lt;string&gt; instance to the pool
    /// </summary>
    public static void ReturnStringList(List<string> list)
    {
        _stringListPool.Return(list);
    }

    /// <summary>
    /// Gets a pooled StringBuilder instance
    /// </summary>
    public static StringBuilder GetStringBuilder()
    {
        return _stringBuilderPool.Get();
    }

    /// <summary>
    /// Returns a StringBuilder instance to the pool
    /// </summary>
    public static void ReturnStringBuilder(StringBuilder sb)
    {
        _stringBuilderPool.Return(sb);
    }
}