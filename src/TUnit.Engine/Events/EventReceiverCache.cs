using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Events;

/// <summary>
/// Caches event receiver lookups for improved performance
/// </summary>
internal sealed class EventReceiverCache
{
    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public Type ReceiverType { get; init; }
        public Type TestClassType { get; init; }

        public bool Equals(CacheKey other) =>
            ReceiverType == other.ReceiverType &&
            TestClassType == other.TestClassType;

        public override bool Equals(object? obj) =>
            obj is CacheKey other && Equals(other);

        public override int GetHashCode()
        {
#if NETSTANDARD2_0
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + (ReceiverType?.GetHashCode() ?? 0);
                hash = hash * 23 + (TestClassType?.GetHashCode() ?? 0);
                return hash;
            }
#else
            return HashCode.Combine(ReceiverType, TestClassType);
#endif
        }
    }

    // Use object to store typed caches (avoids boxing on retrieval)
    private readonly ConcurrentDictionary<Type, object> _typedCaches = new();

    /// <summary>
    /// Get cached receivers or compute and cache them
    /// </summary>
    public T[] GetApplicableReceivers<T>(
        Type testClassType,
        Func<Type, T[]> factory) where T : class, IEventReceiver
    {
        // Get or create typed cache for this receiver type
        var typedCache = (ConcurrentDictionary<Type, T[]>)_typedCaches.GetOrAdd(
            typeof(T),
            static _ => new ConcurrentDictionary<Type, T[]>());

        // Use typed cache - no boxing/unboxing needed
        return typedCache.GetOrAdd(testClassType, factory);
    }
    
    /// <summary>
    /// Clear the cache
    /// </summary>
    public void Clear()
    {
        _typedCaches.Clear();
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int EntryCount, long EstimatedSize) GetStatistics()
    {
        var entryCount = _typedCaches.Values.Sum(cache =>
        {
            if (cache is System.Collections.ICollection collection)
            {
                return collection.Count;
            }
            return 0;
        });

        var estimatedSize = entryCount * (16 + 8); // Rough estimate: key size + reference

        return (entryCount, estimatedSize);
    }
}