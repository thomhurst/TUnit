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
    
    private readonly ConcurrentDictionary<CacheKey, object[]> _cache = new();
    
    /// <summary>
    /// Get cached receivers or compute and cache them
    /// </summary>
    public T[] GetApplicableReceivers<T>(
        Type testClassType,
        Func<Type, T[]> factory) where T : class, IEventReceiver
    {
        var key = new CacheKey 
        { 
            ReceiverType = typeof(T), 
            TestClassType = testClassType 
        };
        
        var cached = _cache.GetOrAdd(key, _ => 
        {
            var receivers = factory(testClassType);
            // Pre-size array and avoid LINQ Cast + ToArray
            var array = new object[receivers.Length];
            for (int i = 0; i < receivers.Length; i++)
            {
                array[i] = receivers[i];
            }
            return array;
        });
        
        // Cast back to specific type
        var result = new T[cached.Length];
        for (int i = 0; i < cached.Length; i++)
        {
            result[i] = (T)cached[i];
        }
        return result;
    }
    
    /// <summary>
    /// Clear the cache
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int EntryCount, long EstimatedSize) GetStatistics()
    {
        var entryCount = _cache.Count;
        var estimatedSize = entryCount * (16 + 8); // Rough estimate: key size + reference
        
        foreach (var kvp in _cache)
        {
            estimatedSize += kvp.Value.Length * 8; // References
        }
        
        return (entryCount, estimatedSize);
    }
}