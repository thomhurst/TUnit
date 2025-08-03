using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Events;

/// <summary>
/// Efficiently tracks test counts for first/last event detection
/// </summary>
internal sealed class TestCountTracker
{
    private readonly ConcurrentDictionary<string, TestCountInfo> _counts = new();

    private sealed class TestCountInfo
    {
        private int _total;
        private int _executed;

        public void SetTotal(int total) => Interlocked.Exchange(ref _total, total);

        public bool IsFirst() => Interlocked.Increment(ref _executed) == 1;

        public bool IsLast()
        {
            var executed = _executed;
            var total = _total;
            return executed == total && total > 0;
        }

        public int Executed => _executed;
        public int Total => _total;
    }

    /// <summary>
    /// Initialize total count for a key
    /// </summary>
    public void InitializeCounts(string key, int totalTests)
    {
        _counts.GetOrAdd(key, _ => new TestCountInfo()).SetTotal(totalTests);
    }

    /// <summary>
    /// Batch initialize counts from test contexts
    /// </summary>
    public void InitializeFromContexts(IEnumerable<TestContext> contexts)
    {
        var contextList = contexts.ToList();

        // Session level
        InitializeCounts("session", contextList.Count);

        // Assembly level
        foreach (var assemblyGroup in contextList.Where(c => c.ClassContext != null).GroupBy(c => c.ClassContext!.AssemblyContext.Assembly.GetName().FullName))
        {
            if (assemblyGroup.Key != null)
            {
                InitializeCounts($"assembly:{assemblyGroup.Key}", assemblyGroup.Count());
            }
        }

        // Class level
        foreach (var classGroup in contextList.Where(c => c.ClassContext != null).GroupBy(c => c.ClassContext!.ClassType))
        {
            if (classGroup.Key != null)
            {
                InitializeCounts($"class:{classGroup.Key.FullName}", classGroup.Count());
            }
        }
    }

    /// <summary>
    /// Check if this is the first test for the given key
    /// </summary>
    public bool CheckIsFirst(string key)
    {
        if (_counts.TryGetValue(key, out var info))
        {
            return info.IsFirst();
        }
        return false;
    }

    /// <summary>
    /// Check if this is the last test for the given key
    /// </summary>
    public bool CheckIsLast(string key)
    {
        if (_counts.TryGetValue(key, out var info))
        {
            return info.IsLast();
        }
        return false;
    }

    /// <summary>
    /// Check both first and last status in one call
    /// </summary>
    public (bool isFirst, bool isLast) CheckTestPosition(string key)
    {
        if (!_counts.TryGetValue(key, out var info))
        {
            return (false, false);
        }

        var isFirst = info.IsFirst();
        var isLast = info.IsLast();

        return (isFirst, isLast);
    }

    /// <summary>
    /// Get progress for a key
    /// </summary>
    public (int executed, int total) GetProgress(string key)
    {
        if (_counts.TryGetValue(key, out var info))
        {
            return (info.Executed, info.Total);
        }
        return (0, 0);
    }

    /// <summary>
    /// Get all keys being tracked
    /// </summary>
    public IEnumerable<string> GetTrackedKeys()
    {
        return _counts.Keys;
    }
}
